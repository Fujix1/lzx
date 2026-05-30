using System.Diagnostics;

namespace MdxUnlzx;

public sealed class MainForm : Form
{
    private const string InitialStatus = "mdx, pdx ファイルやフォルダをドロップしてください";

    private readonly ListView fileList = new();
    private readonly Button clearButton = new();
    private readonly Button decodeButton = new();
    private readonly StatusStrip statusStrip = new();
    private readonly ToolStripStatusLabel statusLabel = new();
    private readonly Dictionary<string, DroppedFile> files = new(StringComparer.OrdinalIgnoreCase);
    private bool isAnalyzing;

    public MainForm()
    {
        Text = "UnLZX for MDX / PDX";
        MinimumSize = new Size(760, 420);
        Size = new Size(920, 560);
        StartPosition = FormStartPosition.CenterScreen;
        AllowDrop = true;

        InitializeControls();

        DragEnter += OnDragEnter;
        DragDrop += OnDragDrop;
    }

    private void InitializeControls()
    {
        fileList.Dock = DockStyle.Fill;
        fileList.View = View.Details;
        fileList.FullRowSelect = true;
        fileList.GridLines = true;
        fileList.HideSelection = false;
        fileList.AllowDrop = true;
        fileList.ShowItemToolTips = true;
        fileList.Columns.Add("ファイル名", 280);
        fileList.Columns.Add("圧縮サイズ", 110, HorizontalAlignment.Right);
        fileList.Columns.Add("解凍サイズ", 110, HorizontalAlignment.Right);
        fileList.Columns.Add("タグ", 330);
        fileList.Columns.Add("LZX圧縮", 90, HorizontalAlignment.Center);
        fileList.DragEnter += OnDragEnter;
        fileList.DragDrop += OnDragDrop;

        clearButton.Text = "クリア";
        clearButton.Dock = DockStyle.Left;
        clearButton.Width = 120;
        clearButton.Enabled = false;
        clearButton.Click += OnClearClick;

        decodeButton.Text = "解凍";
        decodeButton.Dock = DockStyle.Right;
        decodeButton.Width = 120;
        decodeButton.Enabled = false;
        decodeButton.Click += OnDecodeClick;

        var buttonPanel = new Panel
        {
            Dock = DockStyle.Bottom,
            Height = 52,
            Padding = new Padding(8)
        };
        buttonPanel.Controls.Add(clearButton);
        buttonPanel.Controls.Add(decodeButton);

        statusLabel.Text = InitialStatus;
        statusLabel.Spring = true;
        statusLabel.TextAlign = ContentAlignment.MiddleLeft;
        statusStrip.Items.Add(statusLabel);

        Controls.Add(fileList);
        Controls.Add(buttonPanel);
        Controls.Add(statusStrip);
    }

    private void OnDragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = e.Data?.GetDataPresent(DataFormats.FileDrop) == true
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private async void OnDragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is not string[] droppedPaths)
        {
            return;
        }

        await AddPathsAsync(droppedPaths);
    }

    private async Task AddPathsAsync(IEnumerable<string> paths)
    {
        if (isAnalyzing)
        {
            SetStatus("解析中です。完了してから追加してください");
            return;
        }

        isAnalyzing = true;
        UpdateButtonState();
        SetStatus("ドロップされたファイルを収集中...");

        var candidates = await Task.Run(() => ExpandDroppedPaths(paths).ToList());
        var added = 0;
        var analyzed = 0;
        var pendingItems = new List<ListViewItem>();

        fileList.BeginUpdate();
        try
        {
            foreach (var path in candidates)
            {
                if (files.ContainsKey(path))
                {
                    continue;
                }

                SetStatus($"解析済み数: {analyzed}\n解析中: {path}");

                DroppedFile info;
                try
                {
                    info = await Task.Run(() => DroppedFile.Read(path));
                }
                catch (Exception ex)
                {
                    info = DroppedFile.FromError(path, ex.Message);
                }

                files[path] = info;
                pendingItems.Add(CreateItem(info));
                added++;
                analyzed++;

                if (analyzed % 25 == 0)
                {
                    await Task.Yield();
                }
            }
        }
        finally
        {
            if (pendingItems.Count > 0)
            {
                fileList.Items.AddRange(pendingItems.ToArray());
            }
            fileList.EndUpdate();

            isAnalyzing = false;
            UpdateButtonState();
            SetStatus(added == 0
                ? "追加できる mdx, pdx ファイルはありませんでした"
                : $"{added} 件のファイルを追加しました");
        }
    }

    private static IEnumerable<string> ExpandDroppedPaths(IEnumerable<string> paths)
    {
        foreach (var path in paths)
        {
            if (Directory.Exists(path))
            {
                foreach (var file in Directory.EnumerateFiles(path, "*.*", SearchOption.AllDirectories))
                {
                    if (IsSupportedExtension(file))
                    {
                        yield return file;
                    }
                }
                continue;
            }

            if (File.Exists(path) && IsSupportedExtension(path))
            {
                yield return path;
            }
        }
    }

    private static bool IsSupportedExtension(string path)
    {
        var extension = Path.GetExtension(path);
        return extension.Equals(".mdx", StringComparison.OrdinalIgnoreCase)
            || extension.Equals(".pdx", StringComparison.OrdinalIgnoreCase);
    }

    private static ListViewItem CreateItem(DroppedFile info)
    {
        var item = new ListViewItem(Path.GetFileName(info.Path));
        item.SubItems.Add(FormatSize(info.CompressedSize));
        item.SubItems.Add(info.DecodedSize is null ? "" : FormatSize(info.DecodedSize.Value));
        item.SubItems.Add(info.Tag);
        item.SubItems.Add(info.IsLzxCompressed ? "✔" : "");
        item.Tag = info.Path;

        if (info.ErrorMessage is not null)
        {
            item.ForeColor = Color.Firebrick;
            item.ToolTipText = info.ErrorMessage;
        }

        return item;
    }

    private void OnClearClick(object? sender, EventArgs e)
    {
        files.Clear();
        fileList.Items.Clear();
        UpdateButtonState();
        SetStatus(InitialStatus);
    }

    private void OnDecodeClick(object? sender, EventArgs e)
    {
        var targets = files.Values
            .Where(file => file.IsLzxCompressed && file.ErrorMessage is null)
            .ToList();

        if (targets.Count == 0)
        {
            SetStatus("解凍対象のLZX圧縮ファイルがありません");
            return;
        }

        decodeButton.Enabled = false;
        var succeeded = 0;
        var failed = 0;

        foreach (var target in targets)
        {
            try
            {
                SetStatus($"解凍中: {Path.GetFileName(target.Path)}");
                FileConverter.DecodeInPlace(target.Path);
                succeeded++;
                MarkDecoded(target.Path);
            }
            catch (Exception ex)
            {
                failed++;
                MarkFailed(target.Path, ex.Message);
            }
        }

        UpdateButtonState();
        SetStatus($"解凍完了: 成功 {succeeded} 件 / 失敗 {failed} 件");
    }

    private void MarkDecoded(string path)
    {
        if (!files.TryGetValue(path, out var oldInfo))
        {
            return;
        }

        var updated = oldInfo with { IsLzxCompressed = false, ErrorMessage = null };
        files[path] = updated;

        foreach (ListViewItem item in fileList.Items)
        {
            if (!path.Equals(item.Tag as string, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            item.SubItems[4].Text = "";
            item.ForeColor = SystemColors.WindowText;
            item.ToolTipText = "";
            break;
        }
    }

    private void MarkFailed(string path, string message)
    {
        if (files.TryGetValue(path, out var oldInfo))
        {
            files[path] = oldInfo with { ErrorMessage = message };
        }

        foreach (ListViewItem item in fileList.Items)
        {
            if (!path.Equals(item.Tag as string, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            item.ForeColor = Color.Firebrick;
            item.ToolTipText = message;
            break;
        }
    }

    private void SetStatus(string message)
    {
        statusLabel.Text = message;
        statusStrip.Refresh();
        Debug.WriteLine(message);
    }

    private void UpdateButtonState()
    {
        clearButton.Enabled = !isAnalyzing && files.Count > 0;
        decodeButton.Enabled = !isAnalyzing && files.Values.Any(file => file.IsLzxCompressed && file.ErrorMessage is null);
    }

    private static string FormatSize(long bytes)
    {
        return bytes.ToString("N0") + " B";
    }
}
