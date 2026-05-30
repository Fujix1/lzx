namespace MdxUnlzx;

public sealed record DroppedFile(
    string Path,
    long CompressedSize,
    long? DecodedSize,
    string Tag,
    bool IsLzxCompressed,
    string? ErrorMessage)
{
    public static DroppedFile Read(string path)
    {
        var bytes = File.ReadAllBytes(path);
        var extension = System.IO.Path.GetExtension(path);
        var isMdx = extension.Equals(".mdx", StringComparison.OrdinalIgnoreCase);
        var tag = isMdx ? MdxFile.ReadTitle(bytes) : "(PDX)";
        var isCompressed = isMdx
            ? MdxFile.TryGetBodyOffset(bytes, out var bodyOffset) && Lzx042Decoder.LooksLikeLzxContainer(bytes.AsSpan(bodyOffset))
            : Lzx042Decoder.LooksLikeLzxContainer(bytes);
        long? decodedSize = null;
        string? errorMessage = null;

        if (isCompressed)
        {
            try
            {
                decodedSize = FileConverter.DecodeBytes(bytes, extension).LongLength;
            }
            catch (Exception ex)
            {
                errorMessage = "解凍サイズを取得できません: " + ex.Message;
            }
        }

        return new DroppedFile(path, bytes.LongLength, decodedSize, tag, isCompressed, errorMessage);
    }

    public static DroppedFile FromError(string path, string message)
    {
        return new DroppedFile(path, 0, null, "(読み込みエラー)", false, message);
    }
}
