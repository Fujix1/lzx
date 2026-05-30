namespace MdxUnlzx;

public static class FileConverter
{
    public static void DecodeInPlace(string path)
    {
        var source = File.ReadAllBytes(path);
        var decoded = DecodeBytes(source, Path.GetExtension(path));
        var backupPath = GetAvailableBackupPath(path);
        var tempPath = GetAvailableTempPath(path);

        File.WriteAllBytes(tempPath, decoded);

        var backupCreated = false;
        try
        {
            File.Move(path, backupPath);
            backupCreated = true;
            File.Move(tempPath, path);
        }
        catch
        {
            if (File.Exists(tempPath))
            {
                File.Delete(tempPath);
            }

            if (backupCreated && !File.Exists(path) && File.Exists(backupPath))
            {
                File.Move(backupPath, path);
            }

            throw;
        }
    }

    private static byte[] DecodeBytes(byte[] source, string extension)
    {
        if (extension.Equals(".mdx", StringComparison.OrdinalIgnoreCase))
        {
            var bodyOffset = MdxFile.GetBodyOffset(source);
            var decodedBody = Lzx042Decoder.Decode(source.AsSpan(bodyOffset));
            return ByteUtil.Concat(source.AsSpan(0, bodyOffset), decodedBody);
        }

        if (extension.Equals(".pdx", StringComparison.OrdinalIgnoreCase))
        {
            return Lzx042Decoder.Decode(source);
        }

        throw new InvalidOperationException("対応していないファイル形式です。");
    }

    private static string GetAvailableBackupPath(string path)
    {
        var candidate = path + ".bak";
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var i = 1; ; i++)
        {
            candidate = path + $".bak{i}";
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }

    private static string GetAvailableTempPath(string path)
    {
        var candidate = path + ".tmp";
        if (!File.Exists(candidate))
        {
            return candidate;
        }

        for (var i = 1; ; i++)
        {
            candidate = path + $".tmp{i}";
            if (!File.Exists(candidate))
            {
                return candidate;
            }
        }
    }
}
