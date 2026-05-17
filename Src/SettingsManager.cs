using System.Text.Json;
using System.Text.Json.Serialization;

namespace SevenZipAuto;

internal static class SettingsManager
{
    private static readonly JsonSerializerOptions JsonOpts = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// 直前に読み込み／書き込みした JSON 文字列。Save 時の差分判定に用いる。
    /// 内容に変化が無ければ実際のディスク書き込みを抑止する。
    /// </summary>
    private static string? _lastWrittenJson;

    public static string GetSettingsPath()
    {
        // 単一ファイル発行時の信頼性のため Environment.ProcessPath を優先する
        var exePath = Environment.ProcessPath;
        if (string.IsNullOrEmpty(exePath)) exePath = Application.ExecutablePath;
        var dir = Path.GetDirectoryName(exePath);
        if (string.IsNullOrEmpty(dir)) dir = AppContext.BaseDirectory;
        return Path.Combine(dir, "settings.json");
    }

    public static bool Exists() => File.Exists(GetSettingsPath());

    public static Settings Load()
    {
        var path = GetSettingsPath();
        if (!File.Exists(path))
        {
            Logger.Info($"settings.json 未作成 → 既定値で生成準備 (path={path})");
            _lastWrittenJson = null;
            return CreateDefault();
        }
        try
        {
            var json = File.ReadAllText(path);
            _lastWrittenJson = json;
            var settings = JsonSerializer.Deserialize<Settings>(json, JsonOpts);
            Logger.Info($"settings.json 読み込み成功 (path={path})");
            return settings ?? CreateDefault();
        }
        catch (Exception ex)
        {
            Logger.Error($"settings.json 読み込み失敗 (path={path}) → 既定値で復帰", ex);
            _lastWrittenJson = null;
            return CreateDefault();
        }
    }

    /// <summary>設定ファイル不在時の初期インスタンス。7zG パス自動検出を試みる。</summary>
    public static Settings CreateDefault()
    {
        var s = new Settings();
        var detected = SevenZipFinder.FindSevenZipGuiPath();
        if (!string.IsNullOrEmpty(detected))
        {
            s.SevenZipGuiPath = detected;
        }
        return s;
    }

    public static void Save(Settings settings)
    {
        var path = GetSettingsPath();
        try
        {
            var json = JsonSerializer.Serialize(settings, JsonOpts);

            if (string.Equals(json, _lastWrittenJson, StringComparison.Ordinal))
            {
                // 内容差分なし：書き込み skip
                return;
            }

            File.WriteAllText(path, json);
            _lastWrittenJson = json;
            Logger.Info($"settings.json 書き込み成功 (path={path}, {json.Length} bytes)");
        }
        catch (Exception ex)
        {
            Logger.Error($"settings.json 書き込み失敗 (path={path})", ex);
        }
    }
}
