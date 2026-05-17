using System.Diagnostics;

namespace SevenZipAuto;

internal static class FilerLauncher
{
    public static void OpenFolder(string folderPath, Settings settings)
    {
        var target = ResolveExistingFolder(folderPath);
        if (target == null)
        {
            Logger.Warn($"ファイラ起動先フォルダが見つかりません: {folderPath}");
            return;
        }

        try
        {
            if (settings.Filer == FilerKind.Custom &&
                !string.IsNullOrEmpty(settings.CustomFilerPath) &&
                File.Exists(settings.CustomFilerPath))
            {
                Logger.Info($"任意ファイラ起動: \"{settings.CustomFilerPath}\" \"{target}\"");
                Process.Start(new ProcessStartInfo
                {
                    FileName = settings.CustomFilerPath,
                    Arguments = $"\"{target}\"",
                    UseShellExecute = false,
                });
            }
            else
            {
                Logger.Info($"Explorer 起動: \"{target}\"");
                Process.Start(new ProcessStartInfo
                {
                    FileName = "explorer.exe",
                    Arguments = $"\"{target}\"",
                    UseShellExecute = false,
                });
            }
        }
        catch (Exception ex)
        {
            Logger.Error($"ファイラ起動失敗 (target={target})", ex);
        }
    }

    private static string? ResolveExistingFolder(string folderPath)
    {
        if (!string.IsNullOrEmpty(folderPath) && Directory.Exists(folderPath))
            return folderPath;

        var parent = Path.GetDirectoryName(folderPath);
        if (!string.IsNullOrEmpty(parent) && Directory.Exists(parent))
            return parent;

        return null;
    }
}
