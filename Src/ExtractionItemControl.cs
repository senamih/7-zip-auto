namespace SevenZipAuto;

public partial class ExtractionItemControl : UserControl
{
    public ExtractionItem Item { get; }

    /// <summary>「フォルダを開く」アイコンが押された。</summary>
    public event EventHandler? OpenRequested;

    /// <summary>「×」アイコン（削除）が押された。</summary>
    public event EventHandler? RemoveRequested;

    public ExtractionItemControl(ExtractionItem item)
    {
        Item = item;
        InitializeComponent();

        labelName.Text = item.ArchiveFileName;
        toolTip.SetToolTip(labelName, item.ArchivePath);
        toolTip.SetToolTip(buttonOpen,  $"フォルダを開く\n{item.OutputDir}");
        toolTip.SetToolTip(buttonClose, "一覧から削除");
        toolTip.SetToolTip(labelEntries, "エントリ数");

        buttonOpen.Click  += (_, _) => OpenRequested?.Invoke(this, EventArgs.Empty);
        buttonClose.Click += (_, _) => RemoveRequested?.Invoke(this, EventArgs.Empty);

        item.StateChanged += OnItemStateChanged;
        UpdateView();
    }

    private void OnItemStateChanged(object? sender, EventArgs e)
    {
        if (IsDisposed) return;
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(UpdateView)); } catch (ObjectDisposedException) { }
            return;
        }
        UpdateView();
    }

    private void UpdateView()
    {
        labelStatus.Text = Item.State switch
        {
            ExtractionState.Pending   => "待機中",
            ExtractionState.Running   => "展開中…",
            ExtractionState.Completed => "完了",
            ExtractionState.Failed    => "失敗",
            _ => "",
        };

        labelStatus.ForeColor = Item.State switch
        {
            ExtractionState.Failed    => Color.Firebrick,
            ExtractionState.Completed => Color.SeaGreen,
            _ => SystemColors.ControlText,
        };

        // 圧縮 / 展開後 を 1 セルにまとめる
        labelSize.Text = FormatSize(Item.FileSize, Item.UncompressedSize, Item.InspectionDone);
        toolTip.SetToolTip(labelSize, FormatSizeTooltip(Item.FileSize, Item.UncompressedSize));

        labelEntries.Text = Item.EntryCount is int count ? $"{count} 件" : (Item.InspectionDone ? "—" : "…");

        // 展開先は衝突回避で確定時に変わりうるため、その都度反映する
        toolTip.SetToolTip(buttonOpen, $"フォルダを開く\n{Item.OutputDir}");

        // フォルダを開くは展開後（成功・失敗どちらも部分内容を見られるよう許可）
        buttonOpen.Enabled = Item.State == ExtractionState.Completed
                          || Item.State == ExtractionState.Failed;

        // × は常時有効：展開中でも一覧から外せる
        buttonClose.Enabled = true;
    }

    private static string FormatSize(long? compressed, long? uncompressed, bool inspectionDone)
    {
        if (compressed is not long c) return "—";
        if (uncompressed is long u) return $"{Format(c)} / {Format(u)}";
        return inspectionDone ? Format(c) : $"{Format(c)} / …";
    }

    private static string FormatSizeTooltip(long? compressed, long? uncompressed)
    {
        var c = compressed is long cv ? Format(cv) : "—";
        var u = uncompressed is long uv ? Format(uv) : "—";
        return $"圧縮: {c}\n展開後: {u}";
    }

    private static string Format(long bytes)
    {
        string[] units = { "B", "KB", "MB", "GB", "TB" };
        double value = bytes;
        int unit = 0;
        while (value >= 1024 && unit < units.Length - 1)
        {
            value /= 1024;
            unit++;
        }
        return unit == 0 ? $"{(long)value} {units[unit]}" : $"{value:0.#} {units[unit]}";
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Item.StateChanged -= OnItemStateChanged;
            components?.Dispose();
        }
        base.Dispose(disposing);
    }
}
