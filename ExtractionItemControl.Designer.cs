#nullable enable

namespace SevenZipAuto;

partial class ExtractionItemControl
{
    private System.ComponentModel.IContainer? components = null;
    private TableLayoutPanel tableLayout = null!;
    private Label labelName = null!;
    private Label labelSize = null!;     // "圧縮 / 展開後" を 1 セルに表示
    private Label labelEntries = null!;
    private Label labelStatus = null!;
    private Button buttonOpen = null!;
    private Button buttonClose = null!;
    private ToolTip toolTip = null!;

    private void InitializeComponent()
    {
        components = new System.ComponentModel.Container();
        tableLayout = new TableLayoutPanel();
        labelName = new Label();
        labelSize = new Label();
        labelEntries = new Label();
        labelStatus = new Label();
        buttonOpen = new Button();
        buttonClose = new Button();
        toolTip = new ToolTip(components);

        SuspendLayout();
        tableLayout.SuspendLayout();

        // tableLayout: [name(*)] [size(130)] [entries(60)] [status(75)] [open(36)] [close(36)]
        tableLayout.Dock = DockStyle.Fill;
        tableLayout.ColumnCount = 6;
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent,  100F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 130F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  60F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  75F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  36F));
        tableLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute,  36F));
        tableLayout.RowCount = 1;
        tableLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
        tableLayout.Padding = new Padding(0);
        tableLayout.Controls.Add(labelName,    0, 0);
        tableLayout.Controls.Add(labelSize,    1, 0);
        tableLayout.Controls.Add(labelEntries, 2, 0);
        tableLayout.Controls.Add(labelStatus,  3, 0);
        tableLayout.Controls.Add(buttonOpen,   4, 0);
        tableLayout.Controls.Add(buttonClose,  5, 0);

        // labelName
        labelName.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        labelName.AutoEllipsis = true;
        labelName.TextAlign = ContentAlignment.MiddleLeft;
        labelName.Padding = new Padding(8, 0, 0, 0);

        // labelSize
        labelSize.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        labelSize.TextAlign = ContentAlignment.MiddleRight;
        labelSize.Padding = new Padding(0, 0, 6, 0);
        labelSize.ForeColor = SystemColors.GrayText;

        // labelEntries
        labelEntries.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        labelEntries.TextAlign = ContentAlignment.MiddleRight;
        labelEntries.Padding = new Padding(0, 0, 6, 0);
        labelEntries.ForeColor = SystemColors.GrayText;

        // labelStatus
        labelStatus.Anchor = AnchorStyles.Left | AnchorStyles.Right;
        labelStatus.TextAlign = ContentAlignment.MiddleCenter;

        // buttonOpen — 描画ベースのフォルダアイコン
        buttonOpen.Anchor = AnchorStyles.None;
        buttonOpen.AutoSize = false;
        buttonOpen.Size = new Size(30, 26);
        buttonOpen.Margin = new Padding(2, 2, 2, 2);
        buttonOpen.Text = string.Empty;
        buttonOpen.Image = IconFactory.FolderOpen;
        buttonOpen.ImageAlign = ContentAlignment.MiddleCenter;
        buttonOpen.UseVisualStyleBackColor = true;
        buttonOpen.TabStop = true;

        // buttonClose — 描画ベースの×アイコン
        buttonClose.Anchor = AnchorStyles.None;
        buttonClose.AutoSize = false;
        buttonClose.Size = new Size(30, 26);
        buttonClose.Margin = new Padding(2, 2, 2, 2);
        buttonClose.Text = string.Empty;
        buttonClose.Image = IconFactory.Close;
        buttonClose.ImageAlign = ContentAlignment.MiddleCenter;
        buttonClose.UseVisualStyleBackColor = true;
        buttonClose.TabStop = true;

        // ExtractionItemControl
        Controls.Add(tableLayout);
        BorderStyle = BorderStyle.FixedSingle;
        Margin = new Padding(0, 0, 0, 4);
        Height = 36;

        tableLayout.ResumeLayout(false);
        tableLayout.PerformLayout();
        ResumeLayout(false);
    }
}
