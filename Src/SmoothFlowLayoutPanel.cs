namespace SevenZipAuto;

/// <summary>
/// マウスホイールでのスクロールを既定の「行送り」ではなくピクセル単位の細かい刻みに置き換えた
/// FlowLayoutPanel。行高（36px）の倍数で飛ぶ既定挙動を解消し、なめらかな視覚効果を提供する。
/// </summary>
internal sealed class SmoothFlowLayoutPanel : FlowLayoutPanel
{
    /// <summary>マウスホイールの 1 ノッチあたりの移動量（px）。小さいほど滑らか。</summary>
    private const int PixelsPerWheelNotch = 30;

    public SmoothFlowLayoutPanel()
    {
        DoubleBuffered = true;
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        // e.Delta は 1 ノッチで ±120 が標準。SystemInformation.MouseWheelScrollDelta で正規化。
        var delta = e.Delta * PixelsPerWheelNotch / Math.Max(1, SystemInformation.MouseWheelScrollDelta);
        var newY = -AutoScrollPosition.Y - delta;

        if (newY < 0) newY = 0;
        var maxY = Math.Max(0, DisplayRectangle.Height - ClientSize.Height);
        if (newY > maxY) newY = maxY;

        AutoScrollPosition = new Point(-AutoScrollPosition.X, newY);
    }
}
