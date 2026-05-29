using System.ComponentModel;

namespace SevenZipAuto;

public partial class MainForm : Form
{
    private readonly Settings _settings;
    private readonly List<ExtractionItem> _items = new();
    private readonly Dictionary<ExtractionItem, ExtractionItemControl> _controls = new();

    private bool _missingPathPrompted;
    private bool _allowVisible;

    // 以下はデザイナー非関与の実行時専用プロパティ。WinForms アナライザ(WFO1000)が
    // 既定でシリアライズ対象とみなすため、Hidden を明示してデザイナー連携対象から外す。
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool AllowExit { get; set; }

    /// <summary>ウィンドウもトレイも持たない無人実行モード。
    /// 完了した項目は無条件で一覧から外し、一覧が空になればプロセス終了する。
    /// ShowWindow() を経由したら解除する。</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SilentMode { get; set; }

    /// <summary>デバッグ用：true の間 7zG.exe を常に未検出として扱い、
    /// 実際の検出失敗時とまったく同じ挙動（待機中保持・ガイド表示）を再現する。
    /// ガイドでパスが確定したら解除され通常挙動に戻る。</summary>
    [DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
    public bool SimulateMissingSevenZip { get; set; }

    /// <summary>設定画面 OK 後に発火。Tray コンテキスト等が再構成のトリガに使う。</summary>
    public event EventHandler? SettingsChanged;

    /// <summary>明示的なアプリ終了要求。Tray コンテキストが ExitThread を発動する。</summary>
    public event EventHandler? ExitRequested;

    public MainForm(Settings settings)
    {
        _settings = settings;
        InitializeComponent();
        Text = AppInfo.TitleWithVersion;

        flowPanel.SizeChanged += (_, _) => ResizeItemControls();
        LocationChanged += MainForm_LocationOrSizeChanged;
        SizeChanged += MainForm_LocationOrSizeChanged;

        // ドラッグ＆ドロップ受付（Form / FlowPanel / 空時 labelEmpty すべてで受ける）
        AllowDrop = true;
        DragEnter += MainForm_DragEnter;
        DragDrop  += MainForm_DragDrop;
        flowPanel.AllowDrop = true;
        flowPanel.DragEnter += MainForm_DragEnter;
        flowPanel.DragDrop  += MainForm_DragDrop;
        labelEmpty.AllowDrop = true;
        labelEmpty.DragEnter += MainForm_DragEnter;
        labelEmpty.DragDrop  += MainForm_DragDrop;

        UpdateUiState();
    }

    protected override void SetVisibleCore(bool value)
    {
        if (!_allowVisible) value = false;
        base.SetVisibleCore(value);
    }

    public void RestoreFromSettings()
    {
        var w = _settings.Window;
        if (w == null) return;
        if (w.Width <= 0 || w.Height <= 0) return;

        var rect = new Rectangle(w.Left, w.Top, w.Width, w.Height);
        var onScreen = Screen.AllScreens.Any(s => s.WorkingArea.IntersectsWith(rect));
        if (!onScreen) return;

        StartPosition = FormStartPosition.Manual;
        Location = new Point(w.Left, w.Top);
        Size = new Size(w.Width, w.Height);
    }

    public void ShowWindow()
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(ShowWindow)); } catch (ObjectDisposedException) { }
            return;
        }
        if (IsDisposed) return;

        // 明示的にウィンドウを表示する＝もはや無人実行モードではない
        if (SilentMode)
        {
            Logger.Info("ShowWindow が呼ばれたため SilentMode 解除");
            SilentMode = false;
        }

        _allowVisible = true;
        // Visible 状態と実 Win32 ウィンドウの可視状態のズレで Show が抜けるケースを避けるため、
        // 無条件で Show() を呼ぶ（既に表示済みの場合は副作用なし）。
        Show();

        if (WindowState == FormWindowState.Minimized) WindowState = FormWindowState.Normal;

        var prev = TopMost;
        TopMost = true;
        TopMost = prev;

        Activate();
        BringToFront();
    }

    public void HideWindow()
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(HideWindow)); } catch (ObjectDisposedException) { }
            return;
        }

        SettingsManager.Save(_settings);

        if (!_settings.TrayResident)
        {
            RequestExitInternal();
            return;
        }

        _allowVisible = false;
        if (Visible) Hide();
    }

    private void RequestExitInternal()
    {
        AllowExit = true;
        if (Visible)
        {
            Close();
        }
        else
        {
            ExitRequested?.Invoke(this, EventArgs.Empty);
        }
    }

    public void OpenSettingsDialog() => OpenSettingsDialog(null);

    /// <summary>設定ダイアログを開く。targetScreen が指定されていれば、その画面の中央に配置する
    /// （タスクトレイの右クリックから呼び出された時に、トレイのある画面に表示するためのフック）。</summary>
    public void OpenSettingsDialog(Screen? targetScreen)
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => OpenSettingsDialog(targetScreen))); } catch (ObjectDisposedException) { }
            return;
        }
        ShowSettingsDialog(targetScreen);
    }

    public void AddArchives(IEnumerable<string> paths) => AddArchives(paths, suppressMultiShow: false);

    /// <summary>圧縮ファイルパスをキューに投入する。
    /// 2 件以上の一括投入の場合は当該項目を IsFromMultiBatch=true で追加する
    /// （「複数ファイルが渡された時は自動で一覧から削除しない」設定の判定に使う）。</summary>
    public void AddArchives(IEnumerable<string> paths, bool suppressMultiShow)
    {
        if (InvokeRequired)
        {
            try { BeginInvoke(new Action(() => AddArchives(paths, suppressMultiShow))); } catch (ObjectDisposedException) { }
            return;
        }

        var validPaths = new List<string>();
        foreach (var p in paths)
        {
            if (string.IsNullOrWhiteSpace(p)) continue;
            string full;
            try { full = Path.GetFullPath(p); }
            catch { continue; }
            if (!File.Exists(full)) continue;
            validPaths.Add(full);
        }

        if (validPaths.Count == 0) return;

        var isMultiBatch = validPaths.Count >= 2;

        foreach (var full in validPaths)
        {
            AddArchive(full, isMultiBatch);
        }

        if (!suppressMultiShow && isMultiBatch)
        {
            ShowWindow();
        }

        if (_items.Any(i => i.State == ExtractionState.Pending) && !HasValidSevenZipPath() && !_missingPathPrompted)
        {
            _missingPathPrompted = true;
            BeginInvoke(new Action(PromptMissingSevenZip));
        }
    }

    private void AddArchive(string archivePath, bool isFromMultiBatch)
    {
        Logger.Info($"AddArchive: {archivePath} (isFromMultiBatch={isFromMultiBatch})");
        var item = ExtractionItem.CreateForArchive(archivePath, isFromMultiBatch);
        var control = new ExtractionItemControl(item);
        control.OpenRequested   += (_, _) => OnOpenRequested(item);
        control.RemoveRequested += (_, _) => OnRemoveRequested(item);

        _items.Add(item);
        _controls[item] = control;

        flowPanel.Controls.Add(control);
        ResizeItemControls();
        UpdateUiState();

        item.StateChanged += (_, _) =>
        {
            if (IsDisposed) return;
            if (InvokeRequired)
            {
                try { BeginInvoke(new Action(() => OnItemStateChanged(item))); } catch (ObjectDisposedException) { }
                return;
            }
            OnItemStateChanged(item);
        };

        if (HasValidSevenZipPath())
        {
            var sevenZipExe = ArchiveInspector.Resolve7zExePath(_settings.SevenZipGuiPath);
            item.StartInspection(sevenZipExe);
            item.Start(_settings.SevenZipGuiPath);
        }
        else
        {
            item.StartInspection(null);
        }
    }

    private void OnItemStateChanged(ExtractionItem item)
    {
        if (!_items.Contains(item)) return;
        if (item.State != ExtractionState.Completed && item.State != ExtractionState.Failed) return;

        // 無人実行モード：完了は自動オープン＋削除、失敗は削除のみ。一覧が空になればプロセス終了する。
        if (SilentMode)
        {
            Logger.Info($"SilentMode: 項目を一覧から外す archive={item.ArchiveFileName}, state={item.State}");
            if (item.State == ExtractionState.Completed)
            {
                FilerLauncher.OpenFolder(item.OutputDir, _settings);
            }
            RemoveItem(item);
            return;
        }

        // 通常モード：失敗は一覧に残してユーザーに気づかせる
        if (item.State != ExtractionState.Completed) return;

        // 「残す」モード：AutoRemove=ON + MultiBatch + KeepON のときは何もしない（ファイラも開かない）
        if (_settings.AutoRemoveCompleted && item.IsFromMultiBatch && _settings.KeepListWhenMultiBatch)
        {
            return;
        }

        // 完了時は常にファイラで展開先を開く
        FilerLauncher.OpenFolder(item.OutputDir, _settings);

        // AutoRemoveCompleted=ON のときは一覧からも外す
        if (_settings.AutoRemoveCompleted)
        {
            RemoveItem(item);
        }
    }

    private void OnOpenRequested(ExtractionItem item)
    {
        FilerLauncher.OpenFolder(item.OutputDir, _settings);
        // 「フォルダを開くで自動削除する」設定が ON ならファイラ起動後にこの行を一覧から外す
        if (_settings.AutoRemoveOnOpen)
        {
            RemoveItem(item);
        }
    }

    private void OnRemoveRequested(ExtractionItem item)
    {
        RemoveItem(item);
    }

    private void RemoveItem(ExtractionItem item)
    {
        if (_controls.TryGetValue(item, out var control))
        {
            flowPanel.Controls.Remove(control);
            control.Dispose();
            _controls.Remove(item);
        }
        _items.Remove(item);
        UpdateUiState();

        // 一覧空時の挙動：
        //   SilentMode は AutoCloseOnEmpty に依らず常に閉じる（!TrayResident なのでプロセス終了する）。
        //   通常モードは設定の AutoCloseOnEmpty に従う。
        if (_items.Count == 0 && (SilentMode || _settings.AutoCloseOnEmpty))
        {
            HideWindow();
        }
    }

    private void ClearList()
    {
        // 確認なしで即時クリア（実行中の 7zG プロセスは独立して継続）
        foreach (var item in _items.ToList())
        {
            RemoveItem(item);
        }
    }

    private void ShowSettingsDialog(Screen? targetScreen = null)
    {
        IWin32Window? owner = Visible ? this : null;
        using var dlg = new SettingsForm(_settings);

        if (targetScreen != null)
        {
            // タスクトレイ呼び出し用：呼び出したディスプレイの中央に配置する
            var captured = targetScreen;
            dlg.StartPosition = FormStartPosition.Manual;
            dlg.Load += (_, _) =>
            {
                var wa = captured.WorkingArea;
                dlg.Location = new Point(
                    wa.X + Math.Max(0, (wa.Width  - dlg.Width)  / 2),
                    wa.Y + Math.Max(0, (wa.Height - dlg.Height) / 2));
            };
        }

        if (dlg.ShowDialog(owner) == DialogResult.OK)
        {
            SettingsManager.Save(_settings);
            SettingsChanged?.Invoke(this, EventArgs.Empty);

            ResumePendingExtractions();
        }
    }

    /// <summary>7zG.exe パスが有効になった後、検査をやり直し待機中（Pending）項目を展開開始する。
    /// 設定画面 OK 後／ガイドダイアログでのパス確定後の両方から呼ぶ。</summary>
    private void ResumePendingExtractions()
    {
        if (!HasValidSevenZipPath()) return;

        _missingPathPrompted = false;
        var sevenZipExe = ArchiveInspector.Resolve7zExePath(_settings.SevenZipGuiPath);

        foreach (var item in _items.ToList())
        {
            item.StartInspection(sevenZipExe);
        }

        foreach (var item in _items.Where(i => i.State == ExtractionState.Pending).ToList())
        {
            item.Start(_settings.SevenZipGuiPath);
        }
    }

    /// <summary>7zG.exe 未検出時の導線。サイト誘導／winget／再検出／手動指定を集約した
    /// 専用ダイアログを出す。パスが確定したら保存し待機中項目を展開開始する。
    /// 「今はしない」を選んだ場合は項目を待機中のまま一覧に残す（破棄しない）。</summary>
    private void PromptMissingSevenZip()
    {
        IWin32Window? owner = Visible ? this : null;
        using var dlg = new SevenZipGuideForm();
        if (dlg.ShowDialog(owner) == DialogResult.OK && !string.IsNullOrEmpty(dlg.DetectedPath))
        {
            SimulateMissingSevenZip = false; // 確定したら再現モードを解除し通常挙動へ
            _settings.SevenZipGuiPath = dlg.DetectedPath;
            SettingsManager.Save(_settings);
            SettingsChanged?.Invoke(this, EventArgs.Empty);
            Logger.Info($"ガイドダイアログで 7zG.exe 確定: {dlg.DetectedPath}");
            ResumePendingExtractions();
        }
        else
        {
            Logger.Info("ガイドダイアログ: パス未確定（待機中のまま一覧に残す）");
        }
    }

    private bool HasValidSevenZipPath()
        => !SimulateMissingSevenZip
           && !string.IsNullOrEmpty(_settings.SevenZipGuiPath)
           && File.Exists(_settings.SevenZipGuiPath);

    /// <summary>デバッグ用：通常ウィンドウ表示後に、実経路と同じ未検出ガイドを 1 回出す。
    /// Program から UI スレッド上で呼ばれる。</summary>
    public void DebugShowMissingGuide()
    {
        _missingPathPrompted = true; // 以後ファイル投入時に二重表示しない（実挙動と同じ）
        PromptMissingSevenZip();
    }

    private void MenuItemSettings_Click(object? sender, EventArgs e) => ShowSettingsDialog();
    private void MenuItemClear_Click(object? sender, EventArgs e) => ClearList();

    private void UpdateUiState()
    {
        var empty = _items.Count == 0;
        flowPanel.Visible = !empty;
        labelEmpty.Visible = empty;
        menuItemClear.Enabled = !empty;
    }

    private void ResizeItemControls()
    {
        var width = flowPanel.ClientSize.Width - flowPanel.Padding.Horizontal;
        if (width < 200) width = 200;

        foreach (Control c in flowPanel.Controls)
        {
            c.Width = width;
        }
    }

    private void MainForm_LocationOrSizeChanged(object? sender, EventArgs e)
    {
        if (!Visible) return;
        if (WindowState != FormWindowState.Normal) return;
        if (Bounds.Width <= 0 || Bounds.Height <= 0) return;

        _settings.Window = new WindowPlacement
        {
            Left = Bounds.X,
            Top = Bounds.Y,
            Width = Bounds.Width,
            Height = Bounds.Height,
        };
    }

    private void MainForm_DragEnter(object? sender, DragEventArgs e)
    {
        e.Effect = (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
            ? DragDropEffects.Copy
            : DragDropEffects.None;
    }

    private void MainForm_DragDrop(object? sender, DragEventArgs e)
    {
        if (e.Data?.GetData(DataFormats.FileDrop) is string[] files && files.Length > 0)
        {
            AddArchives(files);
        }
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        if (_settings.TrayResident && !AllowExit && e.CloseReason == CloseReason.UserClosing)
        {
            e.Cancel = true;
            HideWindow();
            return;
        }
        SettingsManager.Save(_settings);
        base.OnFormClosing(e);
    }
}
