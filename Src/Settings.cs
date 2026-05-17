namespace SevenZipAuto;

public sealed class Settings
{
    /// <summary>7zG.exe へのフルパス。空なら未設定扱い。</summary>
    public string SevenZipGuiPath { get; set; } = "";

    /// <summary>タスクトレイに常駐するか。既定 false（窓を閉じればプロセス終了）。</summary>
    public bool TrayResident { get; set; } = false;

    /// <summary>一覧が空になったら自動でウィンドウを閉じる（常駐 ON ならトレイへ Hide、OFF なら exit）。
    /// TrayResident には依存しない独立スイッチ。既定 false。</summary>
    public bool AutoCloseOnEmpty { get; set; } = false;

    /// <summary>展開完了時に自動で一覧から削除する。既定 false。
    /// OFF の時は完了しても一覧に残し、ユーザーが × で削除するまで保持する。
    /// ON にすると、単一ファイル展開時には自動でファイラを開かなくなる（注記参照）。</summary>
    public bool AutoRemoveCompleted { get; set; } = false;

    /// <summary>AutoRemoveCompleted の階下サブ設定。
    /// 一度に複数ファイル（≥2）が渡された場合、当該バッチの項目は自動削除の対象外にする。既定 false。</summary>
    public bool KeepListWhenMultiBatch { get; set; } = false;

    /// <summary>「フォルダを開く」アイコンボタンでファイラを開いた時、その行を一覧から自動削除する。既定 false。</summary>
    public bool AutoRemoveOnOpen { get; set; } = false;

    /// <summary>展開後のフォルダを開くファイラ種別。</summary>
    public FilerKind Filer { get; set; } = FilerKind.Explorer;

    /// <summary>FilerKind.Custom 時に呼び出すファイラ実行ファイルへのフルパス。</summary>
    public string CustomFilerPath { get; set; } = "";

    /// <summary>前回終了時のウィンドウ位置・サイズ。null なら既定位置。</summary>
    public WindowPlacement? Window { get; set; }
}

public enum FilerKind
{
    Explorer = 0,
    Custom = 1,
}

public sealed class WindowPlacement
{
    public int Left { get; set; }
    public int Top { get; set; }
    public int Width { get; set; }
    public int Height { get; set; }
}
