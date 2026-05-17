using System.Reflection;

namespace SevenZipAuto;

/// <summary>アセンブリのバージョン等を取得するヘルパ。</summary>
internal static class AppInfo
{
    public const string Name = "7-Zip-Auto";

    /// <summary>表示用バージョン文字列（例: "1.0.0"）。
    /// csproj の InformationalVersion を優先し、ビルドメタデータ（+xxxx）は除去する。</summary>
    public static string Version
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            var info = asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion;
            if (!string.IsNullOrEmpty(info))
            {
                var plus = info.IndexOf('+');
                return plus >= 0 ? info[..plus] : info;
            }
            var v = asm.GetName().Version;
            return v == null ? "" : $"{v.Major}.{v.Minor}.{v.Build}";
        }
    }

    /// <summary>ウィンドウタイトル用（例: "7-Zip-Auto v1.0.0"）。</summary>
    public static string TitleWithVersion
    {
        get
        {
            var v = Version;
            return string.IsNullOrEmpty(v) ? Name : $"{Name} v{v}";
        }
    }
}
