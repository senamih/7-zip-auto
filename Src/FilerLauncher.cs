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
            MessageBox.Show(
                $"展開先フォルダが見つからないため開けませんでした。\n\n対象: {folderPath}",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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

            var filerName = settings.Filer == FilerKind.Custom && !string.IsNullOrEmpty(settings.CustomFilerPath)
                ? settings.CustomFilerPath
                : "エクスプローラ";
            MessageBox.Show(
                "展開先フォルダを開けませんでした。\n\n" +
                $"対象: {target}\n" +
                $"ファイラ: {filerName}\n\n" +
                "ファイラの起動に失敗しました。本アプリとファイラの管理者権限の有無が" +
                "食い違っている場合など、アクセス権限が原因のことがあります。\n\n" +
                $"詳細: {ex.Message}",
                "7-Zip-Auto", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
