namespace MdxUnlzx;

public sealed record DroppedFile(
    string Path,
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

        return new DroppedFile(path, tag, isCompressed, null);
    }

    public static DroppedFile FromError(string path, string message)
    {
        return new DroppedFile(path, "(読み込みエラー)", false, message);
    }
}
