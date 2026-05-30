namespace MdxUnlzx;

public static class ByteUtil
{
    public static int IndexOf(ReadOnlySpan<byte> source, ReadOnlySpan<byte> needle)
    {
        if (needle.Length == 0)
        {
            return 0;
        }

        for (var i = 0; i + needle.Length <= source.Length; i++)
        {
            if (source.Slice(i, needle.Length).SequenceEqual(needle))
            {
                return i;
            }
        }

        return -1;
    }

    public static int IndexOf(ReadOnlySpan<byte> source, byte value)
    {
        for (var i = 0; i < source.Length; i++)
        {
            if (source[i] == value)
            {
                return i;
            }
        }

        return -1;
    }

    public static byte[] Concat(ReadOnlySpan<byte> first, byte[] second)
    {
        var result = new byte[first.Length + second.Length];
        first.CopyTo(result);
        second.CopyTo(result.AsSpan(first.Length));
        return result;
    }
}
