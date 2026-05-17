using System.Reflection;

namespace SevenZipAuto;

/// <summary>埋め込み app.ico をフォーム／NotifyIcon 用に読み込むヘルパ。</summary>
internal static class AppIcon
{
    private const string ResourceName = "SevenZipAuto.app.ico";

    public static Icon? Load()
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null) return null;
            return new Icon(stream);
        }
        catch
        {
            return null;
        }
    }

    /// <summary>サイズ指定で取得する版（NotifyIcon は 16/32 が望ましい）。</summary>
    public static Icon? Load(Size size)
    {
        try
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ResourceName);
            if (stream == null) return null;
            return new Icon(stream, size);
        }
        catch
        {
            return null;
        }
    }
}
