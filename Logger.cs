namespace SevenZipAuto;

/// <summary>
/// 実行ファイルと同階層の `&lt;exeName&gt;.log` に追記する簡易ロガー。
/// 起動時のパス・作業フォルダ・引数や、7zG/7z 連携、ファイラ起動、各種エラーを記録する。
/// 書き込み失敗（権限不足等）は黙殺するため、ログ自身がアプリ動作に影響しないことを保証する。
/// </summary>
internal static class Logger
{
    private static readonly object _lock = new();
    private static string? _logPath;

    public static string? CurrentPath => _logPath;

    public static void Initialize()
    {
        try
        {
            var exePath = Environment.ProcessPath;
            if (string.IsNullOrEmpty(exePath)) exePath = Application.ExecutablePath;
            var dir = Path.GetDirectoryName(exePath);
            if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
            var name = Path.GetFileNameWithoutExtension(exePath);
            if (string.IsNullOrEmpty(name)) name = "7-Zip-Auto";
            _logPath = Path.Combine(dir, $"{name}.log");
        }
        catch
        {
            _logPath = null;
        }
    }

    public static void Info(string message)  => Write("INFO ", message);
    public static void Warn(string message)  => Write("WARN ", message);
    public static void Error(string message) => Write("ERROR", message);

    public static void Error(string message, Exception ex)
        => Write("ERROR", $"{message} | {ex.GetType().Name}: {ex.Message}");

    private static void Write(string level, string message)
    {
        if (string.IsNullOrEmpty(_logPath)) return;
        var line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] {message}{Environment.NewLine}";
        lock (_lock)
        {
            try
            {
                File.AppendAllText(_logPath, line);
            }
            catch
            {
                // ログ失敗は黙殺（権限不足等）
            }
        }
    }
}
