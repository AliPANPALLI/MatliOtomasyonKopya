using System.IO.Ports;
using ITG.Edge.Application;
using Microsoft.Extensions.Options;

namespace ITG.Edge.Infrastructure.Serial;

/// <summary>
/// Discovery is intentionally restricted to pre-approved RS485 COM ports.
/// Never probe arbitrary industrial COM interfaces.
/// </summary>
public sealed class ItgDeviceDiscoveryService(IOptions<ItgOptions> settings)
{
    private string? _lastWorkingPort;

    public IReadOnlyList<string> GetCandidates()
    {
        var config = settings.Value;
        if (!config.AutoDiscover)
            return [config.PortName];

        var connected = SerialPort.GetPortNames()
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var allow = config.AllowedPorts
            .Where(p => !string.IsNullOrWhiteSpace(p) && connected.Contains(p))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        // Preferred port and last successfully probed port go first if approved.
        string? preferred = _lastWorkingPort ?? config.PortName;
        allow.Sort((a, b) =>
        {
            if (a.Equals(preferred, StringComparison.OrdinalIgnoreCase)) return -1;
            if (b.Equals(preferred, StringComparison.OrdinalIgnoreCase)) return 1;
            return ParseNumber(a).CompareTo(ParseNumber(b));
        });
        return allow;
    }

    public void MarkWorking(string portName) => _lastWorkingPort = portName;
    public void MarkFailed(string portName)
    {
        if (string.Equals(_lastWorkingPort, portName, StringComparison.OrdinalIgnoreCase))
            _lastWorkingPort = null;
    }

    private static int ParseNumber(string name) =>
        name.StartsWith("COM", StringComparison.OrdinalIgnoreCase) &&
        int.TryParse(name.AsSpan(3), out int n) ? n : int.MaxValue;
}
