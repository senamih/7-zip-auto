using System.Drawing.Drawing2D;

namespace SevenZipAuto;

/// <summary>
/// 行ボタン用アイコンの描画。フォントの差異に依存しないよう、
/// Graphics で直接描画した Bitmap を静的に保持する。
/// </summary>
internal static class IconFactory
{
    // 行ボタン（30x26）に対しておおよそ半分サイズの 16x16 で描画する
    private const int RenderSize = 16;

    public static readonly Image FolderOpen = RenderFolderIcon(RenderSize);
    public static readonly Image Close      = RenderCloseIcon(RenderSize);

    private static Image RenderFolderIcon(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var bodyBrush  = new SolidBrush(Color.FromArgb(0xFB, 0xBF, 0x24)); // 黄
        using var tabBrush   = new SolidBrush(Color.FromArgb(0xD9, 0x77, 0x06)); // 濃いオレンジ
        using var outlinePen = new Pen(Color.FromArgb(0x92, 0x40, 0x0E), 1.5f);

        float pad     = size * 0.12f;
        float bodyTop = size * 0.34f;

        // タブ（左上）
        var tabRect = new RectangleF(pad, bodyTop - size * 0.16f, size * 0.42f, size * 0.18f);
        g.FillRectangle(tabBrush, tabRect);

        // 本体
        var bodyRect = new RectangleF(pad, bodyTop, size - 2 * pad, size * 0.54f);
        g.FillRectangle(bodyBrush, bodyRect);
        g.DrawRectangle(outlinePen, bodyRect.X, bodyRect.Y, bodyRect.Width, bodyRect.Height);

        return bmp;
    }

    private static Image RenderCloseIcon(int size)
    {
        var bmp = new Bitmap(size, size);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;

        using var pen = new Pen(Color.FromArgb(0xB9, 0x1C, 0x1C), Math.Max(2.5f, size * 0.13f))
        {
            StartCap = LineCap.Round,
            EndCap = LineCap.Round,
        };

        float pad = size * 0.28f;
        g.DrawLine(pen, pad,        pad,        size - pad, size - pad);
        g.DrawLine(pen, size - pad, pad,        pad,        size - pad);

        return bmp;
    }
}
