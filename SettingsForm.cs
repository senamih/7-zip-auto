namespace SevenZipAuto;

public partial class SettingsForm : Form
{
    public Settings Settings { get; }

    /// <summary>拡張子 → 対応する CheckBox。OK 時に差分判定するため保持。</summary>
    private readonly Dictionary<string, CheckBox> _extCheckboxes = new(StringComparer.OrdinalIgnoreCase);

    public SettingsForm(Settings settings)
    {
        Settings = settings;
        InitializeComponent();

        checkTrayResident.Checked = settings.TrayResident;
        checkAutoClose.Checked = settings.AutoCloseOnEmpty;
        checkAutoRemove.Checked = settings.AutoRemoveCompleted;
        checkKeepMultiBatch.Checked = settings.KeepListWhenMultiBatch;
        checkAutoRemoveOnOpen.Checked = settings.AutoRemoveOnOpen;

        textSevenZipPath.Text = settings.SevenZipGuiPath;
        if (settings.Filer == FilerKind.Custom) radioCustomFiler.Checked = true;
        else radioExplorer.Checked = true;
        textCustomFilerPath.Text = settings.CustomFilerPath;

        UpdateFilerEnabled();
        UpdateKeepMultiBatchEnabled();

        toolTip.SetToolTip(buttonDetectSevenZip,
            "「.7z」等の関連付け、または HKLM/HKCU\\SOFTWARE\\7-Zip\\Path から\n7zG.exe を自動検出してパス欄に反映します。");
        toolTip.SetToolTip(groupAssociations,
            "チェックを変更し OK を押すと、Windows のレジストリ（HKCU 配下）の\n「プログラムから開く」候補に本アプリを登録／解除します。\n設定ファイルへは保存しません。");

        PopulateAssociationCheckboxes();
    }

    /// <summary>関連付け CheckBox を動的生成し、現在のレジストリ状態を反映する。</summary>
    private void PopulateAssociationCheckboxes()
    {
        flowAssociations.SuspendLayout();
        try
        {
            foreach (var ext in FileAssociation.SupportedExtensions)
            {
                var cb = new CheckBox
                {
                    Text = ext,
                    AutoSize = true,
                    Margin = new Padding(2, 2, 2, 2),
                    Checked = FileAssociation.IsAssociated(ext),
                };
                flowAssociations.Controls.Add(cb);
                _extCheckboxes[ext] = cb;
            }
        }
        finally
        {
            flowAssociations.ResumeLayout(true);
        }
    }

    /// <summary>チェック状態とレジストリ状態の差分を適用する。settings.json には保存しない。</summary>
    private void ApplyAssociationChanges()
    {
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) exePath = Application.ExecutablePath;

        var changed = false;
        foreach (var (ext, cb) in _extCheckboxes)
        {
            var current = FileAssociation.IsAssociated(ext);
            if (cb.Checked && !current)
            {
                FileAssociation.Associate(ext, exePath);
                changed = true;
            }
            else if (!cb.Checked && current)
            {
                FileAssociation.Unassociate(ext);
                changed = true;
            }
        }
        if (changed) FileAssociation.NotifyShellChange();
    }

    private void CheckAutoRemove_CheckedChanged(object? sender, EventArgs e) => UpdateKeepMultiBatchEnabled();

    private void UpdateKeepMultiBatchEnabled()
    {
        // サブオプションは親（AutoRemoveCompleted）が ON の時のみ意味を持つ
        checkKeepMultiBatch.Enabled = checkAutoRemove.Checked;
    }

    private void RadioFiler_CheckedChanged(object? sender, EventArgs e) => UpdateFilerEnabled();

    private void UpdateFilerEnabled()
    {
        var custom = radioCustomFiler.Checked;
        textCustomFilerPath.Enabled = custom;
        buttonBrowseFiler.Enabled = custom;
    }

    private void ButtonBrowseSevenZip_Click(object? sender, EventArgs e)
    {
        var initial = textSevenZipPath.Text;
        using var dlg = new OpenFileDialog
        {
            Title = "7zG.exe を選択",
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = string.IsNullOrEmpty(initial) ? "7zG.exe" : Path.GetFileName(initial),
            InitialDirectory = string.IsNullOrEmpty(initial) ? "" : Path.GetDirectoryName(initial) ?? "",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            textSevenZipPath.Text = dlg.FileName;
        }
    }

    private void ButtonDetectSevenZip_Click(object? sender, EventArgs e)
    {
        var detected = SevenZipFinder.FindSevenZipGuiPath();
        if (string.IsNullOrEmpty(detected))
        {
            MessageBox.Show(
                this,
                "7zG.exe を自動検出できませんでした。\n手動でパスを指定してください。",
                "7-Zip-Auto",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }
        else
        {
            textSevenZipPath.Text = detected;
        }
    }

    private void ButtonBrowseFiler_Click(object? sender, EventArgs e)
    {
        var initial = textCustomFilerPath.Text;
        using var dlg = new OpenFileDialog
        {
            Title = "ファイラの実行ファイルを選択",
            Filter = "Executable (*.exe)|*.exe|All files (*.*)|*.*",
            FileName = string.IsNullOrEmpty(initial) ? "" : Path.GetFileName(initial),
            InitialDirectory = string.IsNullOrEmpty(initial) ? "" : Path.GetDirectoryName(initial) ?? "",
        };
        if (dlg.ShowDialog(this) == DialogResult.OK)
        {
            textCustomFilerPath.Text = dlg.FileName;
        }
    }

    private void ButtonOK_Click(object? sender, EventArgs e)
    {
        var sevenZipPath = textSevenZipPath.Text.Trim();
        var filerPath    = textCustomFilerPath.Text.Trim();

        // 7zG.exe の存在チェック（空欄は許容＝未設定として扱う）
        if (!string.IsNullOrEmpty(sevenZipPath) && !File.Exists(sevenZipPath))
        {
            MessageBox.Show(
                this,
                $"7zG.exe が指定されたパスに見つかりません:\n{sevenZipPath}",
                "7-Zip-Auto",
                MessageBoxButtons.OK,
                MessageBoxIcon.Warning);
            textSevenZipPath.Focus();
            textSevenZipPath.SelectAll();
            return;
        }

        // 任意ファイラ選択時の存在チェック
        if (radioCustomFiler.Checked)
        {
            if (string.IsNullOrEmpty(filerPath))
            {
                MessageBox.Show(
                    this,
                    "「任意のファイラ」を選択した場合は実行ファイルのパスを指定してください。",
                    "7-Zip-Auto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                textCustomFilerPath.Focus();
                return;
            }
            if (!File.Exists(filerPath))
            {
                MessageBox.Show(
                    this,
                    $"指定されたファイラ実行ファイルが見つかりません:\n{filerPath}",
                    "7-Zip-Auto",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
                textCustomFilerPath.Focus();
                textCustomFilerPath.SelectAll();
                return;
            }
        }

        Settings.TrayResident          = checkTrayResident.Checked;
        Settings.AutoCloseOnEmpty      = checkAutoClose.Checked;
        Settings.AutoRemoveCompleted   = checkAutoRemove.Checked;
        Settings.KeepListWhenMultiBatch= checkKeepMultiBatch.Checked;
        Settings.AutoRemoveOnOpen      = checkAutoRemoveOnOpen.Checked;
        Settings.SevenZipGuiPath       = sevenZipPath;
        Settings.Filer                 = radioCustomFiler.Checked ? FilerKind.Custom : FilerKind.Explorer;
        Settings.CustomFilerPath       = filerPath;

        // 拡張子の関連付けはレジストリのみ。settings.json には保存しない。
        ApplyAssociationChanges();

        DialogResult = DialogResult.OK;
        Close();
    }

    private void ButtonCancel_Click(object? sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
