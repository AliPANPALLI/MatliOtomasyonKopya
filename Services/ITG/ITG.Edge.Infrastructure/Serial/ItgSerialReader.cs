using System.Diagnostics;
using System.IO.Ports;
using ITG.Edge.Application;
using ITG.Edge.Contracts;
using ITG.Edge.Protocol;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Infrastructure.Serial;

public sealed class ItgSerialReader(
    IOptions<ItgOptions> settings,
    ItgDeviceDiscoveryService discovery,
    ILogger<ItgSerialReader> log) : IItgReader
{
    public Task<IReadOnlyList<SensorReading>> ScanAsync(CancellationToken ct) =>
        Task.Run<IReadOnlyList<SensorReading>>(() => Scan(ct), ct);

    private IReadOnlyList<SensorReading> Scan(CancellationToken ct)
    {
        var opt = settings.Value;
        var candidates = discovery.GetCandidates();
        if (candidates.Count == 0)
        {
            log.LogWarning("No approved RS485 COM port present. Configure ITG:AllowedPorts.");
            return [];
        }

        foreach (string portName in candidates)
        {
            ct.ThrowIfCancellationRequested();
            try
            {
                // One owner for this port: probe and actual scan use the same open session.
                using var port = new SerialPort(portName, opt.BaudRate, Parity.None, 8, StopBits.One)
                { ReadTimeout = 50, WriteTimeout = 2000, Handshake = Handshake.None };
                port.Open();
                var session = new Session(port, ct, log);
                var readings = new List<SensorReading>();
                foreach (int controller in opt.Controllers.Distinct())
                {
                    ct.ThrowIfCancellationRequested();
                    if (controller is < 101 or > 106)
                    {
                        log.LogWarning("Unsupported controller {Controller}", controller);
                        continue;
                    }
                    for (int attempt = 0; attempt < 2; attempt++)
                    {
                        if (session.Select(controller) is null) continue;
                        if (opt.ReadEeprom)
                        {
                            if (!session.Eeprom())
                                log.LogWarning("EEPROM failed controller={Controller}; continuing", controller);
                            if (session.Select(controller) is null) continue;
                        }
                        var controllerReadings = session.Cables(controller);

                        readings.AddRange(controllerReadings);

                        int uniqueSensors = controllerReadings
                            .Select(x => x.SensorKey)
                            .Distinct()
                            .Count();

                        int uniqueMeasurements = controllerReadings
                            .Select(x => new { x.SensorKey, x.LocalMeasuredAt })
                            .Distinct()
                            .Count();

                        log.LogInformation(
                            "ITG CONTROLLER | Controller={Controller} | Readings={Readings} | UniqueSensors={UniqueSensors} | UniqueMeasurements={UniqueMeasurements}",
                            controller,
                            controllerReadings.Count,
                            uniqueSensors,
                            uniqueMeasurements);

                        break;
                    }
                }
                // A syntactically matching DC response is insufficient. Require at least
                // one valid CRC/BCD/temperature reading before accepting a new port.
                int totalUniqueSensors = readings
    .Select(x => new { x.ControllerId, x.SensorKey })
    .Distinct()
    .Count();

                int totalUniqueMeasurements = readings
                    .Select(x => new
                    {
                        x.ControllerId,
                        x.SensorKey,
                        x.LocalMeasuredAt
                    })
                    .Distinct()
                    .Count();

                log.LogInformation(
                    "ITG SCAN DETAIL | Port={Port} | TotalReadings={Readings} | UniqueSensors={UniqueSensors} | UniqueMeasurements={UniqueMeasurements} | Duplicates={Duplicates}",
                    portName,
                    readings.Count,
                    totalUniqueSensors,
                    totalUniqueMeasurements,
                    readings.Count - totalUniqueMeasurements);
                if (readings.Count > 0)
                {
                    discovery.MarkWorking(portName);
                    log.LogInformation("ITG verified on {Port}: {SensorCount} readings", portName, readings.Count);
                    return readings;
                }
                discovery.MarkFailed(portName);
                log.LogWarning("No valid ITG readings on {Port}; trying next approved port", portName);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { throw; }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or InvalidOperationException or TimeoutException)
            {
                discovery.MarkFailed(portName);
                log.LogWarning(ex, "Approved COM port {Port} unavailable; trying next", portName);
            }
        }
        log.LogWarning("ITG device not verified on any allowed port");
        return [];
    }

    private sealed class Session(SerialPort port, CancellationToken ct, ILogger logger)
    {
        private bool _echo;
        private byte[] ReadExact(int length, double timeoutSeconds = 3)
        {
            var clock = Stopwatch.StartNew();
            var data = new List<byte>(length);
            while (data.Count < length && clock.Elapsed.TotalSeconds < timeoutSeconds)
            {
                ct.ThrowIfCancellationRequested();
                int n = Math.Min(4096, length - data.Count);
                byte[] temp = new byte[n];
                try
                {
                    int received = port.Read(temp, 0, n);
                    if (received > 0) data.AddRange(temp.AsSpan(0, received).ToArray());
                }
                catch (TimeoutException) { }
            }
            return data.ToArray();
        }
        // Yarida kalan kablo okumasindan sonra cihazin gondermeye devam ettigi artik baytlar,
        // sonraki controller'in DC cevabiyla karismasin diye hat susana kadar okunup atilir.
        private void DrainInput()
        {
            var clock = Stopwatch.StartNew();
            int drained = 0;
            while (clock.Elapsed.TotalSeconds < 3)
            {
                int received = ReadExact(4096, .3).Length;
                if (received == 0) break;
                drained += received;
            }
            port.DiscardInBuffer();
            if (drained > 0) logger.LogWarning("RS485 input drained after aborted read: {Bytes} bytes", drained);
        }
        private void Send(params byte[] data)
        {
            ct.ThrowIfCancellationRequested();
            port.Write(data, 0, data.Length);
            Thread.Sleep(TimeSpan.FromSeconds(data.Length * 10d / port.BaudRate + .005));
            if (_echo)
            {
                byte[] received = ReadExact(data.Length, .3);
                if (!received.SequenceEqual(data)) logger.LogWarning("RS485 TX echo mismatch");
            }
        }
        public byte? Select(int controller)
        {
            byte address = (byte)(0x65 + controller - 101);
            for (int attempt = 1; attempt <= 5; attempt++)
            {
                ct.ThrowIfCancellationRequested();
                Thread.Sleep(20);
                // OS serial input is deliberately kept here as Python does (only drained after aborted reads).
                Send(address);
                byte[] reply = ReadExact(2, .6);
                if (reply.Length > 0 && reply[0] == address && !_echo)
                {
                    _echo = true;
                    reply = reply.Skip(1).Concat(ReadExact(1, .6)).ToArray();
                    logger.LogInformation("RS485 adapter echo detected");
                }
                if (reply.Length >= 2 && reply[0] == 0xDC) return reply[1];
                Thread.Sleep(1000);
            }
            logger.LogWarning("Controller {Controller} selection failed", controller);
            return null;
        }
        public bool Eeprom()
        {
            Send(0xFA, 0xFA);
            byte[] block = ReadExact(177);
            if (block.Length == 177 && Crc8Calculator.Calculate(block.AsSpan(0, 176)) == block[176])
            { Send(0xE0); return true; }
            return false;
        }
        public IReadOnlyList<SensorReading> Cables(int controller)
        {
            var measurements = new List<SensorReading>();
            Send(0xFE, 0xFE);
            byte[] count = ReadExact(1);
            if (count.Length == 0 || count[0] < 1 || count[0] > 32)
            { logger.LogWarning("Invalid cable count controller={Controller}", controller); DrainInput(); return measurements; }
            bool aborted = false;
            for (int i = 0; i < count[0]; i++)
            {
                byte[] head = ReadExact(5);
                if (head.Length != 5 || head[4] < 1 || head[4] > 31)
                { logger.LogWarning("Invalid cable header controller={Controller} index={CableIndex}", controller, i); aborted = true; break; }
                int size = 59 + 8 * head[4];
                byte[] tail = ReadExact(size - 5);
                if (tail.Length != size - 5)
                { logger.LogWarning("Incomplete cable block controller={Controller} index={CableIndex}", controller, i); aborted = true; break; }
                byte[] block = head.Concat(tail).ToArray();
                if (CableParser.ValidHeaderAndCrc(block)) measurements.AddRange(CableParser.Parse(controller, block));
                else logger.LogWarning("Bad CRC/header controller={Controller} index={CableIndex}", controller, i);
                // Python sends E0 after every complete cable block, even for bad CRC.
                Send(0xE0);
            }
            if (aborted) DrainInput();
            else _ = ReadExact(2, .3);
            return measurements;
        }
    }
}
