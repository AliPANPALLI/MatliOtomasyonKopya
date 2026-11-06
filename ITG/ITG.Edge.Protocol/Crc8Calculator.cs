namespace ITG.Edge.Protocol;

public static class Crc8Calculator
{
    // Python: reflected polynomial 0x8C, initial 0, xorout 0.
    public static byte Calculate(ReadOnlySpan<byte> data)
    {
        byte crc = 0;
        foreach (byte value in data)
        {
            crc ^= value;
            for (int i = 0; i < 8; i++)
                crc = (byte)(((crc & 1) != 0) ? ((crc >> 1) ^ 0x8C) : (crc >> 1));
        }
        return crc;
    }
}
