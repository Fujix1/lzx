namespace MdxUnlzx;

public static class MdxFile
{
    private static readonly byte[] TitleTerminator = { 0x0d, 0x0a, 0x1a };

    public static string ReadTitle(ReadOnlySpan<byte> bytes)
    {
        var titleEnd = ByteUtil.IndexOf(bytes, TitleTerminator);
        if (titleEnd < 0)
        {
            return "(タイトル終端なし)";
        }

        var titleBytes = bytes[..titleEnd];
        var title = Cp932.Decode(titleBytes).Replace('\r', ' ').Replace('\n', ' ').Trim();
        return string.IsNullOrWhiteSpace(title) ? "(無題)" : title;
    }

    public static int GetBodyOffset(ReadOnlySpan<byte> bytes)
    {
        if (!TryGetBodyOffset(bytes, out var offset))
        {
            throw new InvalidOperationException("MDXヘッダを解析できません。");
        }

        return offset;
    }

    public static bool TryGetBodyOffset(ReadOnlySpan<byte> bytes, out int offset)
    {
        offset = 0;
        var titleEnd = ByteUtil.IndexOf(bytes, TitleTerminator);
        if (titleEnd < 0)
        {
            return false;
        }

        var pdxNameStart = titleEnd + TitleTerminator.Length;
        var pdxNameEnd = ByteUtil.IndexOf(bytes[pdxNameStart..], 0x00);
        if (pdxNameEnd < 0)
        {
            return false;
        }

        offset = pdxNameStart + pdxNameEnd + 1;
        return true;
    }
}
