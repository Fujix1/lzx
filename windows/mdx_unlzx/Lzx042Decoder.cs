namespace MdxUnlzx;

public static class Lzx042Decoder
{
    private static readonly byte[] Marker = { 0x7f, 0xff, 0xff, 0x4c };
    private static readonly byte[] Header = { 0x4c, 0x5a, 0x58, 0x20, 0x30, 0x2e };

    public static bool HasLzxMarker(ReadOnlySpan<byte> source)
    {
        return ByteUtil.IndexOf(source, Marker) >= 0;
    }

    public static bool LooksLikeLzxContainer(ReadOnlySpan<byte> source)
    {
        return HasLzxMarker(source) || ByteUtil.IndexOf(source, Header) >= 0;
    }

    public static byte[] Decode(ReadOnlySpan<byte> source)
    {
        var streamOffset = FindStreamOffset(source);
        var reader = new BitReader(source.ToArray(), streamOffset);
        var output = new List<byte>(source.Length * 2);

        while (true)
        {
            if (reader.GetBit() == 1)
            {
                output.Add((byte)reader.GetByte());
                continue;
            }

            if (reader.GetBit() == 0)
            {
                var shortCode = (reader.GetBit() << 1) | reader.GetBit();
                var distanceByte = reader.GetByte();
                CopyFromHistory(output, distanceByte - 256, shortCode + 2);
                continue;
            }

            var hi = reader.GetByte();
            var lo = reader.GetByte();
            var packed = (hi << 8) | lo;
            var offset = unchecked((int)(0xffff0000u | (uint)packed)) >> 3;
            var code = lo & 0x07;

            if (code != 0)
            {
                CopyFromHistory(output, offset, code + 2);
                continue;
            }

            var extra = reader.GetByte();
            if (extra == 0)
            {
                return output.ToArray();
            }

            CopyFromHistory(output, offset, extra + 1);
        }
    }

    private static int FindStreamOffset(ReadOnlySpan<byte> source)
    {
        var pos = 0x24;
        while (pos + 2 + Marker.Length <= source.Length)
        {
            pos += 2;
            if (source.Slice(pos, Marker.Length).SequenceEqual(Marker))
            {
                return pos + Marker.Length;
            }
        }

        var fallback = ByteUtil.IndexOf(source, Marker);
        if (fallback >= 0)
        {
            return fallback + Marker.Length;
        }

        throw new InvalidOperationException("LZX042マーカー 7F FF FF 4C が見つかりません。");
    }

    private static void CopyFromHistory(List<byte> output, int offset, int length)
    {
        var sourceIndex = output.Count + offset;
        if (offset >= 0 || sourceIndex < 0)
        {
            throw new InvalidOperationException($"不正な後方参照オフセットです: {offset}");
        }

        for (var i = 0; i < length; i++)
        {
            output.Add(output[sourceIndex++]);
        }
    }

    private sealed class BitReader
    {
        private readonly byte[] source;
        private int offset;
        private int bitCount = 8;
        private int current;

        public BitReader(byte[] source, int offset)
        {
            this.source = source;
            this.offset = offset;
            current = GetByte();
        }

        public int GetByte()
        {
            if (offset >= source.Length)
            {
                throw new InvalidOperationException("LZX042ストリームが途中で終了しました。");
            }

            return source[offset++];
        }

        public int GetBit()
        {
            bitCount--;
            if (bitCount < 0)
            {
                current = GetByte();
                bitCount = 7;
            }

            var bit = (current & 0x80) >> 7;
            current = (current << 1) & 0xff;
            return bit;
        }
    }
}
