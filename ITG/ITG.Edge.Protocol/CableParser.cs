using ITG.Edge.Contracts;
namespace ITG.Edge.Protocol;

public static class CableParser
{
    private static readonly HashSet<double> Invalid = [85d, -127d, 127.9375d];
    public static IReadOnlyList<SensorReading> Parse(int controllerId, byte[] block)
    {
        if (block.Length < 67)
            return [];
        int n = block[4];
        if (n < 1 || n > 31 || block.Length != 59 + 8 * n)
            return [];
        if (block[2] != 0 || block[3] != 0 || block[5] != 0 || block[6] != 0 || block[7] != 0)
            return [];
        if (Crc8Calculator.Calculate(block.AsSpan(0, block.Length - 1)) != block[^1])
            return [];
        byte depot = block[0], cable = block[1];
        var result = new List<SensorReading>();
        for (int j = 0; j < n; j++)
        {
            int offset = 58 + j * 8;
            short raw = unchecked((short)((block[offset] << 8) | block[offset + 1]));
            double temperature = raw / 16d;
            DateTime? measuredAt = BcdDateParser.Parse(block.AsSpan(offset + 2, 6));
            if (Invalid.Contains(temperature) || temperature < -100 || temperature > 150 || measuredAt is null) continue;
            result.Add(new SensorReading(controllerId, measuredAt.Value, $"D{depot:00}_K{cable}_S{j + 1:00}", temperature));
        }
        return result;
    }
    public static bool ValidHeaderAndCrc(byte[] block)
    {
        if (block.Length < 67 || block[4] < 1 || block[4] > 31 || block.Length != 59 + 8 * block[4]) return false;
        return block[2] == 0 && block[3] == 0 && block[5] == 0 && block[6] == 0 && block[7] == 0 &&
            Crc8Calculator.Calculate(block.AsSpan(0, block.Length - 1)) == block[^1];
    }
}
