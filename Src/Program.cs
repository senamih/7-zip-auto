using System.IO.Pipes;

namespace SevenZipAuto;

internal static class Program
{
    private const string MutexName = "7-Zip-Auto_Mutex_4D8C7B6A-9F1E-4C2D-A3B5-6E7F8A9B0C1D";
    private const string PipeName  = "7-Zip-Auto_Pipe_4D8C7B6A-9F1E-4C2D-A3B5-6E7F8A9B0C1D";

    [STAThread]
    private static void Main(string[] args)
    {
        Logger.Initialize();
        Logger.Info("=========== 7-Zip-Auto 起動 ===========");
        Logger.Info($"ExecutablePath  : {Application.ExecutablePath}");
        Logger.Info($"Environment.ProcessPath : {Environment.ProcessPath}");
        Logger.Info($"AppContext.BaseDirectory: {AppContext.BaseDirectory}");
        Logger.Info($"CurrentDirectory: {Environment.CurrentDirectory}");
        Logger.Info($"Args ({args.Length} 件): {(args.Length == 0 ? "(なし)" : string.Join(" | ", args))}");
        Logger.Info($"LogPath         : {Logger.CurrentPath}");

        // デバッグ用：検出失敗シミュレーションモード。
        // `7-Zip-Auto.exe --test-guide` で「7zG.exe が見つからなかった時」と同じ状態を再現する。
        // 単体表示して終了するのではなく、通常どおりウィンドウを開いたうえでガイドを出し、
        // 閉じた後もそのまま通常画面が残る（投入ファイルは待機中のまま保持される）。
        // 実フラグは args から除外し、通常の引数解釈・起動判定に影響させない。
        var simulateMissing = args.Any(a => string.Equals(a, "--test-guide", StringComparison.OrdinalIgnoreCase));
        var appArgs = simulateMissing
            ? args.Where(a => !string.Equals(a, "--test-guide", StringComparison.OrdinalIgnoreCase)).ToArray()
            : args;
        if (simulateMissing)
        {
            Logger.Info("テストモード: 7zG.exe 未検出を再現して通常起動する（--test-guide）");
        }

        using var mutex = new Mutex(initiallyOwned: true, MutexName, out bool createdNew);

        if (!createdNew)
        {
            Logger.Info("Mutex 取得失敗 → 既存インスタンスへ引数転送");
            ForwardArgsToRunningInstance(appArgs);
            Logger.Info("=========== 7-Zip-Auto 終了（多重起動側） ===========");
            return;
        }

        Logger.Info("Mutex 取得成功 → 単一インスタンスとして起動");

        ApplicationConfiguration.Initialize();

        var firstRun = !SettingsManager.Exists();
        Logger.Info($"firstRun = {firstRun}, settings.json = {SettingsManager.GetSettingsPath()}");

        var settings = SettingsManager.Load();
        if (firstRun)
        {
            Logger.Info("初回起動：既定設定を保存");
            SettingsManager.Save(settings);
        }

        Logger.Info($"Settings: TrayResident={settings.TrayResident}, AutoCloseOnEmpty={settings.AutoCloseOnEmpty}, " +
                    $"AutoRemoveCompleted={settings.AutoRemoveCompleted}, KeepListWhenMultiBatch={settings.KeepListWhenMultiBatch}, " +
                    $"AutoRemoveOnOpen={settings.AutoRemoveOnOpen}, SevenZipGuiPath=\"{settings.SevenZipGuiPath}\", " +
                    $"Filer={settings.Filer}, CustomFilerPath=\"{settings.CustomFilerPath}\"");

        var mainForm = new MainForm(settings);
        _ = mainForm.Handle;
        mainForm.RestoreFromSettings();

        var appContext = new TrayApplicationContext(mainForm, settings);

        var hasArgs = appArgs.Length > 0;
        var (showWindow, showSettings) = GetStartupUiActions(firstRun, hasArgs, settings.TrayResident);

        if (simulateMissing)
        {
            // 検出失敗の再現：常駐設定や引数に関わらず必ず通常ウィンドウを出し、
            // 設定ダイアログや SilentMode は無効化する（実際の未検出時の見え方に合わせる）。
            showWindow = true;
            showSettings = false;
            mainForm.SimulateMissingSevenZip = true;
        }
        Logger.Info($"Startup UI: showWindow={showWindow}, showSettings={showSettings}, simulateMissing={simulateMissing}");

        // 無人実行モード判定：ウィンドウも設定画面も出さず、かつタスクトレイ常駐もしない状態。
        // この場合、展開が一通り終わり一覧が空になった時点でプロセスを終了する。
        mainForm.SilentMode = !showWindow && !showSettings && !settings.TrayResident;
        if (mainForm.SilentMode)
        {
            Logger.Info("SilentMode 有効：展開完了後に自動終了する");
        }

        if (showWindow || showSettings)
        {
            mainForm.BeginInvoke(new Action(() =>
            {
                if (showWindow) mainForm.ShowWindow();
                if (showSettings) mainForm.OpenSettingsDialog();
                // 通常ウィンドウを出した直後に、未検出ガイドを実経路と同じ形で表示する。
                if (simulateMissing) mainForm.DebugShowMissingGuide();
            }));
        }

        if (hasArgs)
        {
            // 完全な初回起動 + 引数あり = ウィンドウ非表示で silent 処理する要件のため、
            // 通常の「2件以上 → 自動表示」ルールを抑止する。
            mainForm.AddArchives(appArgs, suppressMultiShow: firstRun);
        }

        StartPipeServer(mainForm);

        Logger.Info("Application.Run 開始");
        Application.Run(appContext);
        Logger.Info("Application.Run 終了");
        Logger.Info("=========== 7-Zip-Auto 終了 ===========");

        GC.KeepAlive(mutex);
    }

    /// <summary>
    /// 単一インスタンス側（Mutex 取得成功）の起動時 UI 判定。
    /// - 完全な初回起動 + 引数なし: ウィンドウ + 設定画面
    /// - 完全な初回起動 + 引数あり: 何も出さず silent 処理
    /// - 設定済み + 引数あり: 何も出さず（一覧UIは AddArchives 内のルールで自動表示判定）
    /// - 設定済み + 引数なし + 常駐 OFF: ウィンドウ表示（D&D 受け皿）
    /// - 設定済み + 引数なし + 常駐 ON: 非表示 silent（トレイへ常駐するのみ。
    ///                                  次回以降の多重起動から窓を出す経路で表示される）
    /// </summary>
    private static (bool showWindow, bool showSettings) GetStartupUiActions(bool firstRun, bool hasArgs, bool trayResident)
    {
        if (firstRun)
        {
            return hasArgs ? (false, false) : (true, true);
        }
        if (hasArgs) return (false, false);
        // 設定済み + 引数なし
        if (trayResident) return (false, false); // 常駐 ON：そのままトレイ常駐へ
        return (true, false);                     // 常駐 OFF：窓を出して D&D を待つ
    }

    /// <summary>
    /// 多重起動側から既存インスタンスへ引数を転送する。
    /// 引数が空の場合は接続のみ行い、Pipe サーバ側で「ウィンドウ表示要求」として扱う。
    /// </summary>
    private static void ForwardArgsToRunningInstance(string[] args)
    {
        try
        {
            using var client = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
            client.Connect(2000);
            using var writer = new StreamWriter(client) { AutoFlush = true };

            int sent = 0;
            foreach (var raw in args)
            {
                if (string.IsNullOrWhiteSpace(raw)) continue;
                string resolved;
                try { resolved = Path.GetFullPath(raw); }
                catch { resolved = raw; }
                writer.WriteLine(resolved);
                sent++;
            }
            writer.Flush();
            Logger.Info(sent == 0
                ? "Pipe: 引数 0 件で接続のみ送信（ShowWindow 要求）"
                : $"Pipe: {sent} 件のパスを既存インスタンスへ転送");
        }
        catch (Exception ex)
        {
            Logger.Error("Pipe 経由の引数転送に失敗", ex);
        }
    }

    private static void StartPipeServer(MainForm mainForm)
    {
        var thread = new Thread(() =>
        {
            while (!mainForm.IsDisposed)
            {
                try
                {
                    using var server = new NamedPipeServerStream(
                        PipeName,
                        PipeDirection.In,
                        maxNumberOfServerInstances: 1,
                        PipeTransmissionMode.Byte,
                        PipeOptions.None);

                    server.WaitForConnection();

                    var paths = new List<string>();
                    using (var reader = new StreamReader(server))
                    {
                        string? line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            if (!string.IsNullOrWhiteSpace(line))
                                paths.Add(line);
                        }
                    }

                    if (!mainForm.IsDisposed)
                    {
                        try
                        {
                            if (paths.Count > 0)
                            {
                                Logger.Info($"Pipe 受信: {paths.Count} 件のパス → AddArchives");
                                mainForm.BeginInvoke(new Action(() => mainForm.AddArchives(paths)));
                            }
                            else
                            {
                                Logger.Info("Pipe 受信: 引数 0 件 → ShowWindow 要求");
                                mainForm.BeginInvoke(new Action(mainForm.ShowWindow));
                            }
                        }
                        catch (ObjectDisposedException) { return; }
                        catch (InvalidOperationException) { return; }
                    }
                }
                catch
                {
                    Thread.Sleep(200);
                }
            }
        })
        {
            IsBackground = true,
            Name = "7-Zip-Auto.PipeServer",
        };
        thread.Start();
    }
}
