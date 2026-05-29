using System.Diagnostics;

namespace SevenZipAuto;

public enum ExtractionState
{
    Pending,
    Running,
    Completed,
    Failed,
}

/// <summary>
/// 1 アーカイブの展開タスクを表す。Process（7zG ハンドル）を保持して
/// 進行中であることを表現し、終了時に StateChanged を発火する。
/// FileSize/EntryCount はインスペクションが完了した時点で値が入り、
/// その時にも StateChanged が発火する（UI 側で表示更新するため）。
/// </summary>
public sealed class ExtractionItem
{
    public string ArchivePath { get; }
    public string OutputDir { get; private set; }
    public Process? Process { get; private set; }
    public ExtractionState State { get; private set; } = ExtractionState.Pending;
    public int ExitCode { get; private set; }

    public long? FileSize { get; private set; }            // 圧縮ファイルそのもののバイト数
    public long? UncompressedSize { get; private set; }    // 展開後の総バイト数
    public int? EntryCount { get; private set; }
    public bool InspectionDone { get; private set; }
    private bool _inspectionInProgress;

    /// <summary>一度に複数件（≥2）が投入された一括バッチの一員かどうか。
    /// 設定「複数ファイルが渡された時は自動で一覧から削除しない」の判定に使う。</summary>
    public bool IsFromMultiBatch { get; init; }

    public event EventHandler? StateChanged;

    public string ArchiveFileName => Path.GetFileName(ArchivePath);

    public ExtractionItem(string archivePath, string outputDir)
    {
        ArchivePath = archivePath;
        OutputDir = outputDir;
    }

    public static ExtractionItem CreateForArchive(string archivePath, bool isFromMultiBatch = false)
    {
        var fullPath = Path.GetFullPath(archivePath);
        var dir = Path.GetDirectoryName(fullPath);
        if (string.IsNullOrEmpty(dir)) dir = Directory.GetCurrentDirectory();
        var name = Path.GetFileNameWithoutExtension(fullPath);
        if (string.IsNullOrEmpty(name)) name = "extracted";
        var outputDir = Path.Combine(dir, name);
        return new ExtractionItem(fullPath, outputDir) { IsFromMultiBatch = isFromMultiBatch };
    }

    /// <summary>
    /// ファイルサイズは即時取得し、エントリ数はバックグラウンドで取得する。
    /// 何度呼ばれても安全（取得済の値は再取得せず、進行中の処理は重複起動しない）。
    /// 取得値が更新されるたびに StateChanged を発火。
    /// </summary>
    public void StartInspection(string? sevenZipExePath)
    {
        if (FileSize == null)
        {
            FileSize = ArchiveInspector.GetFileSize(ArchivePath);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }

        if (EntryCount.HasValue) return;
        if (_inspectionInProgress) return;
        if (string.IsNullOrEmpty(sevenZipExePath)) return; // 7z.exe 未取得時は後で再試行

        _inspectionInProgress = true;
        Task.Run(() =>
        {
            var result = ArchiveInspector.Inspect(ArchivePath, sevenZipExePath);
            if (result != null)
            {
                EntryCount = result.EntryCount;
                UncompressedSize = result.UncompressedTotal;
            }
            InspectionDone = true;
            _inspectionInProgress = false;
            StateChanged?.Invoke(this, EventArgs.Empty);
        });
    }

    public void Start(string sevenZipGuiPath)
    {
        if (State != ExtractionState.Pending) return;

        try
        {
            // 展開先フォルダ名が既存ファイルと衝突する場合（例: test.pptx.zip →
            // フォルダ test.pptx を作ろうとするが同名ファイル test.pptx が既にある）は
            // 連番付きの未使用パスへ退避する。確定後に UI へ反映するため発火。
            var resolved = ResolveOutputDir(OutputDir);
            if (!string.Equals(resolved, OutputDir, StringComparison.Ordinal))
            {
                Logger.Info($"展開先が既存ファイルと衝突したため変更: {OutputDir} → {resolved}");
                OutputDir = resolved;
                StateChanged?.Invoke(this, EventArgs.Empty);
            }
            Directory.CreateDirectory(OutputDir);
        }
        catch (Exception ex)
        {
            Logger.Error($"展開先ディレクトリ作成失敗: {OutputDir}", ex);
            UpdateState(ExtractionState.Failed);
            return;
        }

        try
        {
            var args = $"x \"{ArchivePath}\" -o\"{OutputDir}\" -spe -y";
            Logger.Info($"7zG.exe 起動（展開）: {sevenZipGuiPath} {args}");

            var psi = new ProcessStartInfo
            {
                FileName = sevenZipGuiPath,
                Arguments = args,
                UseShellExecute = false,
                CreateNoWindow = false,
            };

            var proc = new Process { StartInfo = psi, EnableRaisingEvents = true };
            proc.Exited += OnProcessExited;
            Process = proc;

            UpdateState(ExtractionState.Running);
            proc.Start();
            Logger.Info($"7zG.exe 起動成功: pid={proc.Id}, archive={ArchiveFileName}");
        }
        catch (Exception ex)
        {
            Logger.Error($"7zG.exe 起動失敗: archive={ArchivePath}", ex);
            UpdateState(ExtractionState.Failed);
        }
    }

    /// <summary>
    /// 展開先フォルダ名が既存の<b>ファイル</b>と衝突する場合は " (2)", " (3)" … と
    /// 連番を付けて、ファイルにもフォルダにも衝突しない未使用パスを返す。
    /// 既存フォルダがあるだけ／何も無い場合は引数をそのまま返し、従来どおり
    /// そこへ展開（マージ）する動作を保つ。
    /// </summary>
    private static string ResolveOutputDir(string preferred)
    {
        // 何も無い、または既存フォルダがある → そのまま使う（後者は CreateDirectory が no-op）
        if (!File.Exists(preferred)) return preferred;

        // 同名ファイルが存在 → 連番付きの未使用パスを探す
        var dir = Path.GetDirectoryName(preferred) ?? Directory.GetCurrentDirectory();
        var name = Path.GetFileName(preferred);
        for (int i = 2; ; i++)
        {
            var candidate = Path.Combine(dir, $"{name} ({i})");
            if (!File.Exists(candidate) && !Directory.Exists(candidate))
                return candidate;
        }
    }

    private void OnProcessExited(object? sender, EventArgs e)
    {
        try { ExitCode = Process?.ExitCode ?? -1; }
        catch { ExitCode = -1; }
        var ok = ExitCode == 0;
        Logger.Info($"7zG.exe 終了: archive={ArchiveFileName}, exitCode={ExitCode}, result={(ok ? "OK" : "FAIL")}");
        UpdateState(ok ? ExtractionState.Completed : ExtractionState.Failed);
    }

    private void UpdateState(ExtractionState newState)
    {
        State = newState;
        StateChanged?.Invoke(this, EventArgs.Empty);
    }
}
