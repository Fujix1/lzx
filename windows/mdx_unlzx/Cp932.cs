using System.Runtime.InteropServices;

namespace MdxUnlzx;

public static class Cp932
{
    private const uint Cp932CodePage = 932;

    public static string Decode(ReadOnlySpan<byte> bytes)
    {
        if (bytes.IsEmpty)
        {
            return string.Empty;
        }

        var input = bytes.ToArray();
        var charCount = MultiByteToWideChar(Cp932CodePage, 0, input, input.Length, null, 0);
        if (charCount <= 0)
        {
            return BitConverter.ToString(input).Replace("-", " ");
        }

        var chars = new char[charCount];
        var written = MultiByteToWideChar(Cp932CodePage, 0, input, input.Length, chars, chars.Length);
        return written > 0
            ? new string(chars, 0, written)
            : BitConverter.ToString(input).Replace("-", " ");
    }

    [DllImport("kernel32.dll", SetLastError = true)]
    private static extern int MultiByteToWideChar(
        uint codePage,
        uint flags,
        byte[] multiByteStr,
        int multiByteCount,
        [Out] char[]? wideCharStr,
        int wideCharCount);
}
