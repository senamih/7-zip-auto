#nullable enable

namespace SevenZipAuto;

partial class SettingsForm
{
    private System.ComponentModel.IContainer? components = null;

    private CheckBox checkTrayResident = null!;
    private CheckBox checkAutoClose = null!;
    private CheckBox checkAutoRemove = null!;
    private CheckBox checkKeepMultiBatch = null!;
    private CheckBox checkAutoRemoveOnOpen = null!;
    private GroupBox groupAssociations = null!;
    private FlowLayoutPanel flowAssociations = null!;
    private GroupBox groupSevenZip = null!;
    private TextBox textSevenZipPath = null!;
    private Button buttonBrowseSevenZip = null!;
    private Button buttonDetectSevenZip = null!;
    private Label labelSevenZipDescription = null!;
    private Label labelSevenZipDetectHelp = null!;
    private GroupBox groupFiler = null!;
    private RadioButton radioExplorer = null!;
    private RadioButton radioCustomFiler = null!;
    private TextBox textCustomFilerPath = null!;
    private Button buttonBrowseFiler = null!;
    private Button buttonOK = null!;
    private Button buttonCancel = null!;
    private ToolTip toolTip = null!;
    private LinkLabel linkAbout = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        checkTrayResident = new CheckBox();
        checkAutoClose = new CheckBox();
        checkAutoRemove = new CheckBox();
        checkKeepMultiBatch = new CheckBox();
        checkAutoRemoveOnOpen = new CheckBox();
        groupAssociations = new GroupBox();
        flowAssociations = new FlowLayoutPanel();
        groupSevenZip = new GroupBox();
        labelSevenZipDescription = new Label();
        labelSevenZipDetectHelp = new Label();
        textSevenZipPath = new TextBox();
        buttonBrowseSevenZip = new Button();
        buttonDetectSevenZip = new Button();
        groupFiler = new GroupBox();
        radioExplorer = new RadioButton();
        radioCustomFiler = new RadioButton();
        textCustomFilerPath = new TextBox();
        buttonBrowseFiler = new Button();
        buttonOK = new Button();
        buttonCancel = new Button();
        toolTip = new ToolTip(components);
        linkAbout = new LinkLabel();

        SuspendLayout();
        groupSevenZip.SuspendLayout();
        groupFiler.SuspendLayout();
        groupAssociations.SuspendLayout();

        // checkTrayResident
        checkTrayResident.AutoSize = true;
        checkTrayResident.Text = "タスクトレイに常駐する";
        checkTrayResident.Location = new Point(16, 14);

        // checkAutoClose（TrayResident に依存しない独立スイッチ。階下のサブオプションではない）
        checkAutoClose.AutoSize = true;
        checkAutoClose.Text = "一覧が空になったらウィンドウを自動で閉じる";
        checkAutoClose.Location = new Point(16, 38);

        // checkAutoRemove
        checkAutoRemove.AutoSize = true;
        checkAutoRemove.Text = "展開完了時に自動で一覧から削除する";
        checkAutoRemove.Location = new Point(16, 62);
        checkAutoRemove.CheckedChanged += CheckAutoRemove_CheckedChanged;

        // checkKeepMultiBatch — AutoRemove の階下サブオプション（インデント）
        checkKeepMultiBatch.AutoSize = true;
        checkKeepMultiBatch.Text = "複数ファイルが渡された時は自動で一覧から削除しない";
        checkKeepMultiBatch.Location = new Point(36, 86);

        // checkAutoRemoveOnOpen — フォルダを開くボタンで自動削除
        checkAutoRemoveOnOpen.AutoSize = true;
        checkAutoRemoveOnOpen.Text = "「フォルダを開く」ボタン押下時に自動で一覧から削除する";
        checkAutoRemoveOnOpen.Location = new Point(16, 110);

        // groupSevenZip
        groupSevenZip.Text = "7-Zip 実行ファイル (7zG.exe)";
        groupSevenZip.Location = new Point(16, 140);
        groupSevenZip.Size = new Size(520, 116);
        groupSevenZip.Controls.Add(labelSevenZipDescription);
        groupSevenZip.Controls.Add(textSevenZipPath);
        groupSevenZip.Controls.Add(buttonBrowseSevenZip);
        groupSevenZip.Controls.Add(buttonDetectSevenZip);
        groupSevenZip.Controls.Add(labelSevenZipDetectHelp);

        // labelSevenZipDescription
        labelSevenZipDescription.Text = "7-Zip GUI 展開実行ファイル「7zG.exe」のフルパスを指定します。";
        labelSevenZipDescription.AutoSize = true;
        labelSevenZipDescription.Location = new Point(12, 22);

        // textSevenZipPath
        textSevenZipPath.Location = new Point(12, 50);
        textSevenZipPath.Size = new Size(320, 23);

        // buttonBrowseSevenZip
        buttonBrowseSevenZip.Text = "参照…";
        buttonBrowseSevenZip.Location = new Point(340, 49);
        buttonBrowseSevenZip.Size = new Size(75, 26);
        buttonBrowseSevenZip.Click += ButtonBrowseSevenZip_Click;

        // buttonDetectSevenZip
        buttonDetectSevenZip.Text = "検出";
        buttonDetectSevenZip.Location = new Point(423, 49);
        buttonDetectSevenZip.Size = new Size(75, 26);
        buttonDetectSevenZip.Click += ButtonDetectSevenZip_Click;

        // labelSevenZipDetectHelp — 検出ボタンの説明文
        labelSevenZipDetectHelp.Text = "「検出」: レジストリの 7-Zip 関連付け／インストールパスから 7zG.exe を自動検索";
        labelSevenZipDetectHelp.AutoSize = true;
        labelSevenZipDetectHelp.Location = new Point(12, 84);
        labelSevenZipDetectHelp.ForeColor = SystemColors.GrayText;

        // groupFiler
        groupFiler.Text = "展開後にフォルダを開くファイラ";
        groupFiler.Location = new Point(16, 266);
        groupFiler.Size = new Size(520, 132);
        groupFiler.Controls.Add(radioExplorer);
        groupFiler.Controls.Add(radioCustomFiler);
        groupFiler.Controls.Add(textCustomFilerPath);
        groupFiler.Controls.Add(buttonBrowseFiler);

        // radioExplorer
        radioExplorer.AutoSize = true;
        radioExplorer.Text = "エクスプローラ";
        radioExplorer.Location = new Point(12, 26);
        radioExplorer.CheckedChanged += RadioFiler_CheckedChanged;

        // radioCustomFiler
        radioCustomFiler.AutoSize = true;
        radioCustomFiler.Text = "任意のファイラ";
        radioCustomFiler.Location = new Point(12, 56);
        radioCustomFiler.CheckedChanged += RadioFiler_CheckedChanged;

        // textCustomFilerPath
        textCustomFilerPath.Location = new Point(34, 86);
        textCustomFilerPath.Size = new Size(378, 23);

        // buttonBrowseFiler
        buttonBrowseFiler.Text = "参照…";
        buttonBrowseFiler.Location = new Point(420, 85);
        buttonBrowseFiler.Size = new Size(80, 26);
        buttonBrowseFiler.Click += ButtonBrowseFiler_Click;

        // groupAssociations — 右カラム：拡張子の関連付け
        groupAssociations.Text = "拡張子の関連付け（OK で適用）";
        groupAssociations.Location = new Point(552, 14);
        groupAssociations.Size = new Size(216, 384);
        groupAssociations.Controls.Add(flowAssociations);

        // flowAssociations — 拡張子 CheckBox を縦並びで動的追加する FlowLayoutPanel
        flowAssociations.Dock = DockStyle.Fill;
        flowAssociations.FlowDirection = FlowDirection.TopDown;
        flowAssociations.WrapContents = false;
        flowAssociations.AutoScroll = true;
        flowAssociations.Padding = new Padding(10, 6, 6, 6);

        // buttonOK
        buttonOK.Text = "OK";
        buttonOK.Size = new Size(96, 30);
        buttonOK.Location = new Point(560, 436);
        buttonOK.Click += ButtonOK_Click;

        // buttonCancel
        buttonCancel.Text = "キャンセル";
        buttonCancel.Size = new Size(96, 30);
        buttonCancel.Location = new Point(664, 436);
        buttonCancel.Click += ButtonCancel_Click;

        // linkAbout — 左下にソフト名＋バージョン。クリックで GitHub を開く
        linkAbout.AutoSize = true;
        linkAbout.Text = AppInfo.TitleWithVersion;
        linkAbout.Location = new Point(16, 444);
        linkAbout.LinkClicked += LinkAbout_LinkClicked;
        toolTip.SetToolTip(linkAbout, AboutUrl);

        // SettingsForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(776, 478);
        FormBorderStyle = FormBorderStyle.FixedDialog;
        MinimizeBox = false;
        MaximizeBox = false;
        ShowInTaskbar = false;
        StartPosition = FormStartPosition.CenterParent;
        Text = "7-Zip-Auto 設定";
        AcceptButton = buttonOK;
        CancelButton = buttonCancel;
        var appIcon = AppIcon.Load();
        if (appIcon != null) Icon = appIcon;

        Controls.Add(checkTrayResident);
        Controls.Add(checkAutoClose);
        Controls.Add(checkAutoRemove);
        Controls.Add(checkKeepMultiBatch);
        Controls.Add(checkAutoRemoveOnOpen);
        Controls.Add(groupSevenZip);
        Controls.Add(groupFiler);
        Controls.Add(groupAssociations);
        Controls.Add(buttonOK);
        Controls.Add(buttonCancel);
        Controls.Add(linkAbout);

        groupSevenZip.ResumeLayout(false);
        groupSevenZip.PerformLayout();
        groupFiler.ResumeLayout(false);
        groupFiler.PerformLayout();
        groupAssociations.ResumeLayout(false);
        groupAssociations.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
