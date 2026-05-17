namespace SevenZipAuto;

/// <summary>
/// アプリのライフサイクルを司る。
/// - Settings.TrayResident=true: NotifyIcon 可視化、× で Hide、トレイ「終了」で実終了。
/// - Settings.TrayResident=false: NotifyIcon 非表示、ウィンドウ閉じ＝アプリ終了。
/// 設定はランタイムに切替可能（MainForm.SettingsChanged でアイコンの可視状態を更新）。
/// </summary>
internal sealed class TrayApplicationContext : ApplicationContext
{
    private readonly System.ComponentModel.IContainer _components;
    private readonly NotifyIcon _notifyIcon;
    private readonly MainForm _mainForm;
    private readonly Settings _settings;
    private bool _exiting;

    public TrayApplicationContext(MainForm mainForm, Settings settings)
    {
        _mainForm = mainForm;
        _settings = settings;
        _components = new System.ComponentModel.Container();

        var menu = new ContextMenuStrip(_components);
        menu.Items.Add("表示(&S)", null, (_, _) => _mainForm.ShowWindow());
        menu.Items.Add("設定(&E)…", null, (_, _) =>
        {
            // 呼び出し時のカーソル位置からスクリーンを判定し、その画面の中央に設定ダイアログを表示
            var screen = Screen.FromPoint(Cursor.Position);
            _mainForm.OpenSettingsDialog(screen);
        });
        menu.Items.Add(new ToolStripSeparator());
        menu.Items.Add("終了(&X)", null, (_, _) => RequestExit());

        var trayIcon = AppIcon.Load(new Size(16, 16))
                    ?? AppIcon.Load()
                    ?? SystemIcons.Application;

        _notifyIcon = new NotifyIcon(_components)
        {
            Icon = trayIcon,
            Text = "7-Zip-Auto",
            Visible = settings.TrayResident,
            ContextMenuStrip = menu,
        };

        // 単一クリック（左ボタン）でウィンドウを表示。右クリックは ContextMenuStrip が処理。
        _notifyIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                _mainForm.ShowWindow();
            }
        };

        _mainForm.SettingsChanged += (_, _) => RefreshFromSettings();
        _mainForm.FormClosed += (_, _) => CleanupAndExit();
        _mainForm.ExitRequested += (_, _) => RequestExit();
    }

    public void RefreshFromSettings()
    {
        try { _notifyIcon.Visible = _settings.TrayResident; }
        catch { /* dispose 済み等は無視 */ }
    }

    private void RequestExit()
    {
        if (_exiting) return;
        if (!_mainForm.IsDisposed)
        {
            _mainForm.AllowExit = true;
            try { _mainForm.Close(); } catch { }
        }
        CleanupAndExit();
    }

    private void CleanupAndExit()
    {
        if (_exiting) return;
        _exiting = true;
        try
        {
            _notifyIcon.Visible = false;
            _notifyIcon.Dispose();
        }
        catch { }
        ExitThread();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _components?.Dispose();
        base.Dispose(disposing);
    }
}
