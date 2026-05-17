using System.Diagnostics;

namespace SevenZipAuto;

/// <summary>
/// 7zG.exe が未検出のときに表示する導線集約ダイアログ。
/// 「公式サイトを開く／winget でインストール／インストール済みなので再検出／手動で指定／今はしない」
/// を 1 つにまとめ、行き止まりを作らない。
/// 再検出・手動指定でパスが確定した場合は <see cref="DetectedPath"/> に格納し DialogResult.OK で閉じる。
/// </summary>
internal sealed class SevenZipGuideForm : Form
{
    private const string DownloadUrl = "https://www.7-zip.org/";
    private const string WingetId    = "7zip.7zip";

    /// <summary>再検出／手動指定で確定した 7zG.exe のフルパス（未確定なら null）。</summary>
    public string? DetectedPath { get; private set; }

    public SevenZipGuideForm()
    {
        Text            = "7-Zip が見つかりません";
        FormBorderStyle = FormBorderStyle.FixedDialog;
        StartPosition   = FormStartPosition.CenterParent;
        MinimizeBox     = false;
        MaximizeBox     = false;
        ShowInTaskbar   = false;
        ClientSize      = new Size(470, 286);
        Icon            = AppIcon.Load();

        var message = new Label
        {
            AutoSize  = false,
            Location  = new Point(16, 14),
            Size      = new Size(438, 78),
            Text      = "アーカイブの展開に必要な 7-Zip（7zG.exe）が見つかりませんでした。\n\n"
                      + "7-Zip 本体は本アプリに同梱されていません。未インストールの場合は下記から"
                      + "インストールし、「インストール済み → 再検出」を押してください。",
        };

        var btnSite = MakeButton(
            "7-Zip 公式サイトを開く",
            new Point(16, 100),
            "既定のブラウザで https://www.7-zip.org/ を開きます。");
        btnSite.Click += (_, _) => OpenUrl();

        var btnWinget = MakeButton(
            "winget でインストール",
            new Point(16, 138),
            "Windows パッケージマネージャー winget で 7-Zip をインストールします。");
        btnWinget.Click += (_, _) => RunWinget();
        btnWinget.Enabled = IsWingetAvailable();
        if (!btnWinget.Enabled)
            _toolTip.SetToolTip(btnWinget, "winget が見つからないため使用できません。公式サイトからインストールしてください。");

        var btnRedetect = MakeButton(
            "インストール済み → 再検出",
            new Point(16, 176),
            "関連付け／レジストリ／既定インストール先から 7zG.exe を再検索します。");
        btnRedetect.Click += (_, _) => Redetect();

        var btnManual = MakeButton(
            "手動で 7zG.exe を指定",
            new Point(16, 214),
            "7zG.exe のフルパスをファイル選択ダイアログで指定します。");
        btnManual.Click += (_, _) => Manual();

        var btnLater = new Button
        {
            Text         = "今はしない",
            Location     = new Point(354, 250),
            Size         = new Size(100, 28),
            DialogResult = DialogResult.Cancel,
        };

        Controls.AddRange(new Control[]
        {
            message, btnSite, btnWinget, btnRedetect, btnManual, btnLater,
        });
        CancelButton = btnLater;
    }

    private readonly ToolTip _toolTip = new();

    private Button MakeButton(string text, Point location, string tip)
    {
        var b = new Button
        {
            Text      = text,
            Location  = location,
            Size      = new Size(438, 30),
            TextAlign = ContentAlignment.MiddleLeft,
            Padding   = new Padding(8, 0, 0, 0),
        };
        _toolTip.SetToolTip(b, tip);
        return b;
    }

    private void OpenUrl()
    {
        try
        {
            Process.Start(new ProcessStartInfo(DownloadUrl) { UseShellExecute = true });
            Logger.Info($"ガイド: 公式サイトを開く {DownloadUrl}");
        }
        catch (Exception ex)
        {
            Logger.Error("公式サイトを開けませんでした", ex);
            MessageBox.Show(this,
                $"ブラウザを開けませんでした。\n手動で {DownloadUrl} にアクセスしてください。",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private static bool IsWingetAvailable()
    {
        try
        {
            var psi = new ProcessStartInfo("cmd.exe", "/c where winget")
            {
                UseShellExecute        = false,
                CreateNoWindow         = true,
                RedirectStandardOutput = true,
                RedirectStandardError  = true,
            };
            using var p = Process.Start(psi);
            if (p == null) return false;
            p.WaitForExit(3000);
            return p.HasExited && p.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }

    private void RunWinget()
    {
        try
        {
            // 結果を確認できるよう可視ウィンドウで実行し、終了後も残す
            var psi = new ProcessStartInfo("cmd.exe",
                $"/k winget install -e --id {WingetId}")
            {
                UseShellExecute = true,
            };
            Process.Start(psi);
            Logger.Info($"ガイド: winget install -e --id {WingetId} 起動");
            MessageBox.Show(this,
                "winget のインストール画面を起動しました。\n"
                + "インストール完了後、「インストール済み → 再検出」を押してください。",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            Logger.Error("winget の起動に失敗", ex);
            MessageBox.Show(this,
                "winget を起動できませんでした。公式サイトからインストールしてください。",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
    }

    private void Redetect()
    {
        var found = SevenZipFinder.FindSevenZipGuiPath();
        if (!string.IsNullOrEmpty(found))
        {
            DetectedPath = found;
            Logger.Info($"ガイド: 再検出成功 {found}");
            MessageBox.Show(this,
                $"7-Zip を検出しました:\n{found}\n\nこのパスで展開を続行します。",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Information);
            DialogResult = DialogResult.OK;
            Close();
            return;
        }

        MessageBox.Show(this,
            "7zG.exe を検出できませんでした。\n"
            + "7-Zip をインストール済みであれば「手動で 7zG.exe を指定」から登録してください。",
            "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }

    private void Manual()
    {
        using var dlg = new OpenFileDialog
        {
            Title    = "7zG.exe を選択",
            Filter   = "7-Zip GUI (7zG.exe)|7zG.exe|Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = "7zG.exe",
        };
        if (dlg.ShowDialog(this) != DialogResult.OK) return;

        if (!File.Exists(dlg.FileName))
        {
            MessageBox.Show(this,
                $"指定されたファイルが見つかりません:\n{dlg.FileName}",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        DetectedPath = dlg.FileName;
        Logger.Info($"ガイド: 手動指定 {dlg.FileName}");
        DialogResult = DialogResult.OK;
        Close();
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) _toolTip.Dispose();
        base.Dispose(disposing);
    }
}
