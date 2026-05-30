using System.Diagnostics;

namespace SevenZipAuto;

/// <summary>
/// アーカイブのメタ情報（圧縮ファイルサイズ／エントリ数／展開後の総ファイルサイズ）取得ヘルパ。
/// エントリ数・総解凍サイズの取得には 7zG.exe と同フォルダの 7z.exe（CLI 版）を使う。
/// </summary>
internal static class ArchiveInspector
{
    public sealed record InspectResult(int EntryCount, long UncompressedTotal);

    /// <summary>アーカイブ実体の健全性。Unknown は検証手段（7z.exe）が無く判定不能の意。</summary>
    public enum ArchiveIntegrity { Intact, Corrupt, Unknown }

    public static long? GetFileSize(string archivePath)
    {
        try { return new FileInfo(archivePath).Length; }
        catch { return null; }
    }

    /// <summary>7zG.exe のパスから、同フォルダの 7z.exe（CLI 版）パスを推定。</summary>
    public static string? Resolve7zExePath(string? sevenZipGuiPath)
    {
        if (string.IsNullOrEmpty(sevenZipGuiPath)) return null;
        var dir = Path.GetDirectoryName(sevenZipGuiPath);
        if (string.IsNullOrEmpty(dir)) return null;
        var candidate = Path.Combine(dir, "7z.exe");
        return File.Exists(candidate) ? candidate : null;
    }

    /// <summary>
    /// `7z t`（アーカイブテスト）でアーカイブ実体の健全性を検証する。
    /// 7zG.exe は「アーカイブ末尾に余分なデータがある（There are data after the end of
    /// archive）」等の<b>無害な警告</b>でも終了コード 2 を返すことがあり（Google Fonts の
    /// zip 等）、本当に壊れているのか／単なる警告なのかを切り分けるために用いる。
    /// コンソール版 7z の終了コードで判定する（0/1=健全, 2 以上=破損, 実行不能=Unknown）。
    /// </summary>
    public static ArchiveIntegrity VerifyArchive(string archivePath, string? sevenZipExePath)
    {
        if (string.IsNullOrEmpty(sevenZipExePath) || !File.Exists(sevenZipExePath))
        {
            Logger.Warn($"7z.exe 未指定／未存在のため整合性検証を skip: archive={archivePath}");
            return ArchiveIntegrity.Unknown;
        }

        try
        {
            var args = $"t \"{archivePath}\" -y";
            Logger.Info($"7z.exe 起動（整合性検証）: {sevenZipExePath} {args}");

            // 出力は不要なので明示的に握りつぶす（リダイレクトせず CreateNoWindow で破棄）。
            // 標準出力／標準エラーをリダイレクトすると読み残しでデッドロックし得るため避ける。
            var psi = new ProcessStartInfo
            {
                FileName = sevenZipExePath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = true,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                Logger.Warn($"7z.exe Process.Start が null を返却（検証）: archive={archivePath}");
                return ArchiveIntegrity.Unknown;
            }

            if (!proc.WaitForExit(300_000))
            {
                try { proc.Kill(); } catch { }
                Logger.Warn($"7z.exe 整合性検証が5分でタイムアウト: archive={archivePath}");
                return ArchiveIntegrity.Unknown;
            }

            var code = proc.ExitCode;
            var result = code <= 1 ? ArchiveIntegrity.Intact : ArchiveIntegrity.Corrupt;
            Logger.Info($"7z.exe 整合性検証完了: archive={Path.GetFileName(archivePath)}, exitCode={code}, result={result}");
            return result;
        }
        catch (Exception ex)
        {
            Logger.Error($"7z.exe 整合性検証中に例外: archive={archivePath}", ex);
            return ArchiveIntegrity.Unknown;
        }
    }

    /// <summary>
    /// `7z l -slt -ba` を実行し、`Path = ` 行をエントリ数として、`Size = N` 行を加算して
    /// 展開後の総バイト数として集計する。失敗時は null。
    /// </summary>
    public static InspectResult? Inspect(string archivePath, string? sevenZipExePath)
    {
        if (string.IsNullOrEmpty(sevenZipExePath) || !File.Exists(sevenZipExePath))
        {
            Logger.Warn($"7z.exe 未指定／未存在のためアーカイブ解析を skip: archive={archivePath}");
            return null;
        }

        try
        {
            var args = $"l -slt -ba \"{archivePath}\"";
            Logger.Info($"7z.exe 起動（解析）: {sevenZipExePath} {args}");

            var psi = new ProcessStartInfo
            {
                FileName = sevenZipExePath,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = System.Text.Encoding.UTF8,
            };

            using var proc = Process.Start(psi);
            if (proc == null)
            {
                Logger.Warn($"7z.exe Process.Start が null を返却: archive={archivePath}");
                return null;
            }

            int entryCount = 0;
            long total = 0;
            string? line;
            while ((line = proc.StandardOutput.ReadLine()) != null)
            {
                if (line.StartsWith("Path = ", StringComparison.Ordinal))
                {
                    entryCount++;
                }
                else if (line.StartsWith("Size = ", StringComparison.Ordinal))
                {
                    var sizeText = line.AsSpan(7).Trim();
                    if (long.TryParse(sizeText, out var sz) && sz > 0)
                    {
                        total += sz;
                    }
                }
            }
            if (!proc.WaitForExit(60_000))
            {
                try { proc.Kill(); } catch { }
                Logger.Warn($"7z.exe 60秒タイムアウトで中断: archive={archivePath}");
                return null;
            }
            if (proc.ExitCode != 0)
            {
                Logger.Warn($"7z.exe 非ゼロ終了: exitCode={proc.ExitCode}, archive={archivePath}");
                return null;
            }
            Logger.Info($"7z.exe 解析完了: archive={Path.GetFileName(archivePath)}, entries={entryCount}, uncompressed={total}");
            return new InspectResult(entryCount, total);
        }
        catch (Exception ex)
        {
            Logger.Error($"7z.exe 解析中に例外: archive={archivePath}", ex);
            return null;
        }
    }
}
