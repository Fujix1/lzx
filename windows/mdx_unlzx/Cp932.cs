using System.Text;

namespace MdxUnlzx;

public static class Cp932
{
    private static readonly Encoding Encoding = CreateEncoding();

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        return Encoding.GetString(bytes).TrimEnd('\0');
    }

    private static Encoding CreateEncoding()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        return Encoding.GetEncoding(
            932,
            EncoderFallback.ReplacementFallback,
            DecoderFallback.ReplacementFallback);
    }
}
