using Microsoft.Win32;

namespace SevenZipAuto;

/// <summary>レジストリから 7zG.exe のフルパスを推定するヘルパ。</summary>
internal static class SevenZipFinder
{
    private static readonly string[] AssociationExts =
    {
        ".7z", ".zip", ".rar", ".tar", ".gz", ".xz", ".bz2", ".cab", ".iso",
    };

    public static string? FindSevenZipGuiPath()
    {
        var fromAssoc = FindFromAssociation();
        if (!string.IsNullOrEmpty(fromAssoc))
        {
            Logger.Info($"7zG.exe 検出（関連付け経由）: {fromAssoc}");
            return fromAssoc;
        }

        var fromRegistry = FindFromInstallRegistry();
        if (!string.IsNullOrEmpty(fromRegistry))
        {
            Logger.Info($"7zG.exe 検出（インストールレジストリ経由）: {fromRegistry}");
            return fromRegistry;
        }

        Logger.Warn("7zG.exe 自動検出に失敗（関連付け／レジストリいずれも見つからず）");
        return null;
    }

    private static string? FindFromAssociation()
    {
        foreach (var ext in AssociationExts)
        {
            try
            {
                using var extKey = Registry.ClassesRoot.OpenSubKey(ext);
                var progId = extKey?.GetValue("") as string;
                if (string.IsNullOrEmpty(progId)) continue;

                using var cmdKey = Registry.ClassesRoot.OpenSubKey($@"{progId}\shell\open\command");
                var cmd = cmdKey?.GetValue("") as string;
                if (string.IsNullOrEmpty(cmd)) continue;

                var exePath = ExtractExePath(cmd);
                if (string.IsNullOrEmpty(exePath)) continue;

                var dir = Path.GetDirectoryName(exePath);
                if (string.IsNullOrEmpty(dir)) continue;

                var sevenZipG = Path.Combine(dir, "7zG.exe");
                if (File.Exists(sevenZipG)) return sevenZipG;
            }
            catch
            {
                // 次の拡張子を試行
            }
        }
        return null;
    }

    private static string? FindFromInstallRegistry()
    {
        foreach (var hive in new[] { RegistryHive.LocalMachine, RegistryHive.CurrentUser })
        {
            foreach (var view in new[] { RegistryView.Registry64, RegistryView.Registry32 })
            {
                try
                {
                    using var baseKey = RegistryKey.OpenBaseKey(hive, view);
                    using var key = baseKey.OpenSubKey(@"SOFTWARE\7-Zip");
                    var path = key?.GetValue("Path") as string;
                    if (string.IsNullOrEmpty(path)) continue;

                    var exe = Path.Combine(path, "7zG.exe");
                    if (File.Exists(exe)) return exe;
                }
                catch
                {
                    // 次のビューを試行
                }
            }
        }
        return null;
    }

    private static string? ExtractExePath(string command)
    {
        var trimmed = command.Trim();
        if (trimmed.Length == 0) return null;

        if (trimmed[0] == '"')
        {
            var end = trimmed.IndexOf('"', 1);
            return end > 0 ? trimmed[1..end] : null;
        }

        var sp = trimmed.IndexOf(' ');
        return sp > 0 ? trimmed[..sp] : trimmed;
    }
}
