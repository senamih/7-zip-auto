#nullable enable

namespace SevenZipAuto;

partial class MainForm
{
    private System.ComponentModel.IContainer? components = null;
    private MenuStrip menuStrip = null!;
    private ToolStripMenuItem menuItemClear = null!;
    private ToolStripMenuItem menuItemSettings = null!;
    private SmoothFlowLayoutPanel flowPanel = null!;
    private Label labelEmpty = null!;

    protected override void Dispose(bool disposing)
    {
        if (disposing) components?.Dispose();
        base.Dispose(disposing);
    }

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        menuStrip = new MenuStrip();
        menuItemClear = new ToolStripMenuItem();
        menuItemSettings = new ToolStripMenuItem();
        flowPanel = new SmoothFlowLayoutPanel();
        labelEmpty = new Label();

        SuspendLayout();
        menuStrip.SuspendLayout();

        // menuStrip — 「設定」を左端に置く
        menuStrip.Items.Add(menuItemSettings);
        menuStrip.Items.Add(menuItemClear);
        menuStrip.Dock = DockStyle.Top;

        // menuItemClear
        menuItemClear.Text = "リストをクリア(&C)";
        menuItemClear.Click += MenuItemClear_Click;

        // menuItemSettings
        menuItemSettings.Text = "設定(&S)…";
        menuItemSettings.Click += MenuItemSettings_Click;

        // flowPanel
        flowPanel.Dock = DockStyle.Fill;
        flowPanel.FlowDirection = FlowDirection.TopDown;
        flowPanel.WrapContents = false;
        flowPanel.AutoScroll = true;
        flowPanel.Padding = new Padding(8);
        flowPanel.BackColor = SystemColors.Window;

        // labelEmpty — 一覧が空の時に表示する D&D 受付プレースホルダ
        labelEmpty.Dock = DockStyle.Fill;
        labelEmpty.TextAlign = ContentAlignment.MiddleCenter;
        labelEmpty.Text = "ここに圧縮ファイルをドラッグ＆ドロップ\n\n（複数ファイル可）";
        labelEmpty.Font = new Font("Segoe UI", 13F, FontStyle.Regular);
        labelEmpty.ForeColor = SystemColors.GrayText;
        labelEmpty.BackColor = SystemColors.Window;
        labelEmpty.AllowDrop = true;
        labelEmpty.Visible = true;

        // MainForm
        AutoScaleDimensions = new SizeF(7F, 15F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(720, 420);
        MinimumSize = new Size(420, 220);
        Controls.Add(flowPanel);
        Controls.Add(labelEmpty);
        Controls.Add(menuStrip);
        MainMenuStrip = menuStrip;
        Name = "MainForm";
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        Text = "7-Zip-Auto";
        var appIcon = AppIcon.Load();
        if (appIcon != null) Icon = appIcon;

        menuStrip.ResumeLayout(false);
        menuStrip.PerformLayout();
        ResumeLayout(false);
        PerformLayout();
    }
}
