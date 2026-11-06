namespace ITG.Edge.Protocol;

public static class BcdDateParser
{
    // Device order: seconds, minutes, hours, day, month, year(2000+YY).
    public static DateTime? Parse(ReadOnlySpan<byte> data)
    {
        if (data.Length != 6)
            return null;
        Span<int> x = stackalloc int[6];
        for (int i = 0; i < 6; i++)
        {
            int hi = data[i] >> 4, lo = data[i] & 15;
            if (hi > 9 || lo > 9)
                return null;
            x[i] = hi * 10 + lo;
        }
        try
        {
            return new DateTime(2000 + x[5], x[4], x[3], x[2], x[1], x[0], DateTimeKind.Unspecified);
        }
        catch (ArgumentOutOfRangeException)
        {
            return null;
        }
    }
}
