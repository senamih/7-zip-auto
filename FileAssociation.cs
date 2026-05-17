using System.Runtime.InteropServices;
using Microsoft.Win32;

namespace SevenZipAuto;

/// <summary>
/// Windows レジストリ（HKCU\Software\Classes）への ProgID 登録と
/// 拡張子の OpenWithProgids への登録／削除を行うヘルパ。
///
/// HKCU 配下で完結するため管理者権限不要。Win10/11 では本登録はあくまで
/// 「プログラムから開く」候補に追加するもので、既定アプリ化はユーザーが
/// Windows の設定で行う必要があるが、Windows 7/古い 10 等では legacy パスとして
/// 既定の関連付けに反映される場合もある。
/// </summary>
internal static class FileAssociation
{
    /// <summary>HKCU 配下に登録する ProgID（識別子のみ、表示はしない）。</summary>
    public const string ProgId = "SevenZipAuto.Archive";

    /// <summary>HKCU\Software\Classes\&lt;ProgId&gt;\(default) に書き込む表示名。</summary>
    public const string ProgIdDescription = "7-Zip-Auto アーカイブ";

    /// <summary>関連付け候補として提示する拡張子。先頭にドットを付ける。</summary>
    public static readonly IReadOnlyList<string> SupportedExtensions = new[]
    {
        ".7z", ".zip", ".rar", ".tar", ".gz", ".bz2", ".xz",
        ".cab", ".iso", ".lzh", ".arj", ".wim", ".tgz", ".tbz",
    };

    /// <summary>指定拡張子に対し、本アプリ ProgID が OpenWithProgids に登録されているか。</summary>
    public static bool IsAssociated(string ext)
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}\OpenWithProgids");
            if (key == null) return false;
            return key.GetValueNames().Any(n => string.Equals(n, ProgId, StringComparison.OrdinalIgnoreCase));
        }
        catch (Exception ex)
        {
            Logger.Error($"関連付け状態取得失敗: {ext}", ex);
            return false;
        }
    }

    /// <summary>ProgID（ハンドラ）をユーザーレジストリに登録する。冪等。</summary>
    public static void EnsureProgIdRegistered(string exePath)
    {
        try
        {
            using (var progKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}"))
            {
                progKey?.SetValue(string.Empty, ProgIdDescription);
            }
            using (var iconKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\DefaultIcon"))
            {
                iconKey?.SetValue(string.Empty, $"\"{exePath}\",0");
            }
            using (var cmdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ProgId}\shell\open\command"))
            {
                cmdKey?.SetValue(string.Empty, $"\"{exePath}\" \"%1\"");
            }
            Logger.Info($"ProgID 登録/更新: {ProgId} → \"{exePath}\"");
        }
        catch (Exception ex)
        {
            Logger.Error("ProgID 登録失敗", ex);
        }
    }

    /// <summary>拡張子の OpenWithProgids に本アプリ ProgID を追加する。</summary>
    public static void Associate(string ext, string exePath)
    {
        try
        {
            EnsureProgIdRegistered(exePath);
            using var openWith = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{ext}\OpenWithProgids");
            openWith?.SetValue(ProgId, Array.Empty<byte>(), RegistryValueKind.None);
            Logger.Info($"関連付け追加: {ext} += {ProgId}");
        }
        catch (Exception ex)
        {
            Logger.Error($"関連付け追加失敗: {ext}", ex);
        }
    }

    /// <summary>拡張子の OpenWithProgids から本アプリ ProgID を削除する。</summary>
    public static void Unassociate(string ext)
    {
        try
        {
            using var openWith = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{ext}\OpenWithProgids", writable: true);
            if (openWith != null &&
                openWith.GetValueNames().Any(n => string.Equals(n, ProgId, StringComparison.OrdinalIgnoreCase)))
            {
                openWith.DeleteValue(ProgId, throwOnMissingValue: false);
                Logger.Info($"関連付け削除: {ext} -= {ProgId}");
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"関連付け削除失敗: {ext}", ex);
        }
    }

    /// <summary>関連付け変更を Explorer へ通知し、ファイルアイコンやコンテキストメニューを再評価させる。</summary>
    public static void NotifyShellChange()
    {
        try
        {
            SHChangeNotify(SHCNE_ASSOCCHANGED, SHCNF_IDLIST, IntPtr.Zero, IntPtr.Zero);
            Logger.Info("Shell へ関連付け変更通知 (SHCNE_ASSOCCHANGED)");
        }
        catch (Exception ex)
        {
            Logger.Error("SHChangeNotify 失敗", ex);
        }
    }

    [DllImport("shell32.dll")]
    private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    private const int SHCNE_ASSOCCHANGED = 0x08000000;
    private const int SHCNF_IDLIST = 0x0000;
}
