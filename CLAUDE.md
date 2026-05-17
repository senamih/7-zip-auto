
## Claude Code 追加指示
- 日本語応対を行うこと。
- CLAUDELOG.local.logに追記する形で、やりとりが一度完了する度に指示そのままの文言と実際の作業内容を200文字以内程度にまとめること。
  - 次のフォーマットを厳守すること。
    `<改行>---<改行><改行>**指示**: <指示文言><改行><改行>**作業**: <作業まとめ><改行>`
  - ファイルの中身を確認しないこと。
  - ファイルが無い場合は作成すること。

# 7-Zip連携アプリ「7-Zip-Auto」

## 概要
圧縮ファイルを引数または D&D で受け取り、設定済みの 7zG.exe で展開し、展開先フォルダをエクスプローラまたは任意のファイラで開く WinForms 製ユーティリティ。多重起動を防止し、複数ファイル投入時はキュー管理する。タスクトレイ常駐の ON/OFF は設定で切替可能。

## 機能詳細

### 起動・多重起動・常駐
- 引数として圧縮ファイル（複数可）を受ける。ウィンドウへの圧縮ファイルのドラッグ＆ドロップでも受け付ける。
- 多重起動時は引数を起動済みインスタンスの展開キューへ追加し、新規プロセスは即終了する。
  - 引数なしで多重起動された場合は、既存インスタンスのウィンドウを表示する（タスクトレイ常駐中のアクティブ化用途）。
- 「タスクトレイに常駐する」設定（既定 OFF）で挙動を切り替える。
  - 常駐 ON：トレイに NotifyIcon を表示し、ウィンドウを × で閉じても／一覧が空になってもプロセスを継続する。プロセス終了はトレイメニューの「終了」のみ。
  - 常駐 OFF：NotifyIcon は出さず、ウィンドウを閉じる／一覧が空になる＝プロセス終了。
- タスクトレイアイコンは単一の左クリック（MouseClick）でウィンドウを表示する。右クリックではコンテキストメニュー（表示／設定／終了）を出す。
- 無人実行モード（SilentMode）：起動時にウィンドウも設定ダイアログも出さず、かつタスクトレイ常駐もしない構成のときに有効。展開完了／失敗ごとに項目を一覧から外し、一覧が空になればプロセス終了する。展開成功時は対象フォルダを自動でファイラで開く。ウィンドウを途中で表示した場合（多重起動経由の ShowWindow 等）は SilentMode 解除。
- 単一インスタンス起動時（Mutex 取得側）の表示判定：
  - 完全な初回起動（設定ファイル不在）＋引数あり：ウィンドウ非表示で silent 処理し、空時に終了。
  - 完全な初回起動（設定ファイル不在）＋引数なし：ウィンドウを開き、設定画面も自動で開く。
  - 既存設定有り＋常駐 OFF＋引数なし：D&D の受け皿としてウィンドウを開く。
  - 既存設定有り＋常駐 ON＋引数なし：何も出さずトレイ常駐のみで開始（窓を出すのは多重起動経由の表示要求から）。
  - 既存設定有り＋引数あり：silent 処理。
- 多重起動側（Mutex 取得失敗）の挙動：
  - 引数あり：絶対パス化して既存インスタンスへ転送（一覧UIは既存ルールで自動表示判定）。
  - 引数なし：既存インスタンスへ「ウィンドウ表示要求」を送る。トレイ常駐時のアクティブ化として機能する。
- 多重起動経由・D&D 経由いずれの場合も、一度に 2 件以上の圧縮ファイルが投入されたらウィンドウを自動表示する（初回起動 silent 経路を除く）。
- 「一覧が空になったら自動で閉じる」設定（既定 OFF）は TrayResident に依存しない独立スイッチで、ON のとき空時に「常駐 ON ならトレイへ Hide／OFF ならプロセス終了」を行う。OFF のときは空でも何もしない。

### 展開
- 7zG.exe を呼び出してアーカイブを展開する。
- 7zG.exe のパスは設定画面で設定可能。設定ファイル不在の初回起動時、`.7z` 等の関連付けまたは `HKLM/HKCU\SOFTWARE\7-Zip\Path` から自動検出を試みる。
- 設定画面の「検出」ボタンで任意のタイミングでも自動検出を再実行できる（検出失敗時は通知ダイアログを出す）。
- 展開先はアーカイブと同階層の「アーカイブ名（拡張子なし）」フォルダ。
- 7-Zip の `-spe` スイッチを利用し、エントリルートに同名フォルダしかない場合の重複生成を回避する。

### 一覧UI
- 展開中の圧縮ファイルを行で一覧表示する（縦に並ぶ FlowLayout、`SmoothFlowLayoutPanel` でマウスホイールをピクセル単位の細かい刻みに上書きしてスクロールを滑らかにする）。
- 各行は `ファイル名` / `サイズ（圧縮 / 展開後）` / `エントリ数` / `状態` / `フォルダを開くアイコンボタン` / `削除アイコンボタン (×)` の 6 要素で構成する。
- 圧縮ファイルそのもののサイズは `FileInfo` で即時取得、エントリ数および展開後の総ファイルサイズは 7zG.exe と同フォルダの 7z.exe（CLI 版）を `7z l -slt -ba` で起動し、`Path = ` 行のカウントと `Size = ` 行の総和から非同期に取得する。
- 行毎に 7zG プロセスのハンドル (`Process`) を保持し、状態を `待機中` / `展開中…` / `完了` / `失敗` で更新表示する。
- 行ボタンのアイコンは Graphics で直接描画した Bitmap を `Button.Image` に設定する（環境フォントの差で表示不能になるのを避けるため。サイズは行ボタンに対して半分程度の 16px 系）。
- フォルダを開くアイコンボタンを押すと、設定済みファイラで展開先を開く（一覧からは削除しない）。
  - ただし「『フォルダを開く』ボタン押下時に自動で一覧から削除する」設定が ON の場合は、ファイラ起動後にその行を一覧から外す。
- 削除アイコン (×) を押すと、展開中であってもその行を一覧から外す（実行中の 7zG プロセスは独立して継続させる）。
- 展開完了時の挙動（自動オープン＆自動削除）：
  - 完了時は常にファイラで展開先を自動で開く（例外は下記の「残す」モードのみ）。
  - 「展開完了時に自動で一覧から削除する」OFF（既定）：完了しても一覧に残す。ファイラは開く。
  - 同 ON：完了した項目を一覧から外す。一覧から外す前にファイラを開く。
  - 同 ON + 一度に 2 件以上の一括投入 + サブ設定「複数ファイルが渡された時は自動で一覧から削除しない」ON（残すモード）：一覧に残し、ファイラも開かない。
- 一度に 2 件以上が一括投入された項目は `IsFromMultiBatch=true` を保持し、上記サブ設定で挙動を切り替える。
- 一覧が空のときはウィンドウ中央に「ここに圧縮ファイルをドラッグ＆ドロップ」プレースホルダを表示し、当該領域も D&D 受付対象とする。
- メニューに「リストをクリア」を配置する。確認なしで一覧の全項目を即時削除する（実行中の 7zG プロセスは独立して継続）。空時はメニューを無効化。
- メニューの並び順は「設定」を一番左に置く。

### 設定画面
- 設定ダイアログは横 2 カラム構成。左カラムに既存の各種スイッチと 7zG・ファイラ設定、右カラムに「拡張子の関連付け」グループを配置する。
- タスクトレイに常駐する ON/OFF（既定 OFF）
- 一覧が空になったらウィンドウを自動で閉じる ON/OFF（既定 OFF。常駐有無に依存しない独立スイッチ）
- 展開完了時に自動で一覧から削除する ON/OFF（既定 OFF）
  - サブ：複数ファイルが渡された時は自動で一覧から削除しない ON/OFF（既定 OFF。親 ON の時のみ有効）
- 「フォルダを開く」ボタン押下時に自動で一覧から削除する ON/OFF（既定 OFF）
- 7zG.exe のパス（参照ボタン・検出ボタン付き／OK時に存在チェック）
  - 「検出」ボタンの説明文をボタン近傍に表示し、ホバー時のツールチップでも詳細を補足する。
- 開くファイラ：エクスプローラ／任意のファイラ（任意選択時はパス指定可、参照ボタン付き／OK時に存在チェック・空欄不可）
- OK 押下時、設定された 7zG.exe パスや任意ファイラパスが実在しない場合は警告ダイアログを表示してダイアログを閉じない。
- タスクトレイから右クリックで設定を呼び出した場合は、呼び出したディスプレイ（カーソル位置が属するスクリーン）の中央に配置する。
- 拡張子の関連付け（右カラム）：
  - 対象拡張子（.7z / .zip / .rar / .tar / .gz / .bz2 / .xz / .cab / .iso / .lzh / .arj / .wim / .tgz / .tbz）を CheckBox で列挙する。
  - 設定ダイアログを開くタイミングで `HKCU\Software\Classes\<ext>\OpenWithProgids` を読んで現在の関連付け状態を CheckBox に反映する。
  - OK 押下時に差分を `Associate` / `Unassociate` で反映し、`SHChangeNotify(SHCNE_ASSOCCHANGED)` で Explorer に通知する。
  - 関連付け状態は settings.json には保存しない（レジストリのみが真の状態）。
  - ProgID は `SevenZipAuto.Archive`（HKCU 配下、管理者権限不要）。Windows 10/11 では「プログラムから開く」候補への登録となり、既定アプリ化はユーザーが Windows 設定で行う運用とする。

### 設定保存（settings.json）
- 実行ファイルと同階層の `settings.json` に保存。
- 設定値および前回終了時のウィンドウ位置・サイズを保持。設定ファイル有り起動時は前回位置でウィンドウを再現する（実際の表示は起動判定に従う）。
- 設定変更時・ウィンドウ非表示時・実終了時に随時書き出す。直前に書き込んだ／読み込んだ JSON との差分が無い場合は実書き込みを skip する（無駄な I/O・ファイルウォッチャ通知を避ける）。

### ログ出力
- 実行ファイルと同階層に `<実行ファイル名>.log`（既定 `7-Zip-Auto.log`）を追記する簡易ロガーを持つ。
- パス解決は `Environment.ProcessPath`（単一ファイル発行下でも信頼できる）を優先し、取れない場合は `Application.ExecutablePath` → `AppContext.BaseDirectory` の順でフォールバック。
- 記録対象：起動時の各種パス（ExecutablePath / ProcessPath / BaseDirectory / CurrentDirectory）と引数、Mutex 取得結果、初期設定の主要フィールド、SilentMode 判定、settings.json の読み書き結果（成功時は path とサイズ、失敗時は例外内容）、7zG の検出経路、7zG/7z プロセス起動コマンドと終了コード、ファイラ起動、Pipe 経由の引数受信／ShowWindow 要求。
- 書き込み失敗（権限不足等）は黙殺し、ロガー自身がアプリ動作に影響しないことを保証する。

### 命名・配布
- ウィンドウタイトル・実行ファイル名はアプリ名「7-Zip-Auto」を用いる。
- 専用アプリアイコン（マルチ解像度 `app.ico`）を、実行ファイル本体（Win32 リソース）・メインウィンドウ・タスクトレイの NotifyIcon の全てに適用する。
- 発行物は **単一ファイル `7-Zip-Auto.exe` のみ**として `Release/` フォルダに格納する。DLL や PDB を伴わない 1 ファイルにすることを必須とする。
  - csproj に下記プロパティを設定し、`dotnet publish -c Release -o Release` だけで単一ファイル発行が完結する状態にする：
    - `RuntimeIdentifier=win-x64`、`SelfContained=true`、`PublishSingleFile=true`、`IncludeNativeLibrariesForSelfExtract=true`、`IncludeAllContentForSelfExtract=true`、`EnableCompressionInSingleFile=true`、`DebugType=embedded`。
  - 発行前に既存の `Release/`・`bin/`・`obj/` を削除して残骸が混入しないようにする。

## 環境構築（Alpine で dotnet build を通すまで）

- 背景：Alpine の apk パッケージ `dotnet8-sdk`（`/usr/lib/dotnet`、`/usr/bin/dotnet`）は **Linux 向けワークロードのみ**で `Microsoft.NET.Sdk.WindowsDesktop` を含まず、WinForms（`UseWindowsForms=true`）プロジェクトは `WindowsDesktop.targets が見つからない` で失敗する。
- 解決策：**Microsoft 公式配布の .NET SDK（Alpine 用 `linux-musl-x64` ビルド）には WindowsDesktop SDK が含まれる**。これを apk 管理下と衝突しない `/opt/dotnet-ms` へ展開して使う。本環境では構築済み（SDK 8.0.420 / Host 8.0.26）。
- 公式 SDK が musl 上で動くための実行時依存（apk）：`bash curl ca-certificates icu-libs libgcc libstdc++ libintl zlib`。
  ```sh
  apk add --no-cache bash curl ca-certificates icu-libs libgcc libstdc++ libintl zlib
  ```
- 公式 SDK を `/opt/dotnet-ms` へ導入する（再構築時の手順）：
  ```sh
  curl -fsSL https://dot.net/v1/dotnet-install.sh -o /tmp/dotnet-install.sh
  chmod +x /tmp/dotnet-install.sh
  # Alpine は musl。dotnet-install.sh は RID を自動判定（linux-musl-x64）する
  /tmp/dotnet-install.sh --channel 8.0 --quality ga --install-dir /opt/dotnet-ms
  ```
  - 固定バージョンで揃えたい場合は `--version 8.0.420`（`--channel 8.0` の代わり）。
- 導入確認：
  ```sh
  DOTNET_ROOT=/opt/dotnet-ms PATH=/opt/dotnet-ms:$PATH dotnet --info        # RID: linux-musl-x64, SDK 8.0.x
  ls /opt/dotnet-ms/sdk/*/Sdks/ | grep WindowsDesktop                       # Microsoft.NET.Sdk.WindowsDesktop が出れば OK
  ```
- win-x64 自己完結発行に要るランタイムパック（`Microsoft.NETCore.App.Runtime.win-x64` / `Microsoft.WindowsDesktop.App.Runtime.win-x64`）は `/opt/dotnet-ms/packs` には無く、初回 `restore`/`publish` 時に NuGet から取得され `~/.nuget` にキャッシュされる（オフライン環境では事前 restore が必要）。
- システム既定の `dotnet`（apk 版）は触らない。ビルド／発行のたびに下記のとおり `DOTNET_ROOT` と `PATH` で公式 SDK を明示的に指す。

## ビルド・発行（毎回これに従う）

- **実装環境**: Alpine Linux（WSL2 可）。**ターゲット**: Windows 10/11 x64。
- Alpine／Linux 標準の `dotnet8-sdk` には WindowsDesktop SDK が無く、WinForms（`UseWindowsForms`）プロジェクトはビルドできない。**Microsoft 公式 SDK が `/opt/dotnet-ms` に配置済み**（`Microsoft.NET.Sdk.WindowsDesktop` 同梱）。必ずこれを使う。csproj に `<EnableWindowsTargeting>true</EnableWindowsTargeting>` 必須（設定済み）。
- 本プロジェクトは純粋な WinForms ＋ .NET 標準ライブラリのみで、`AllowUnsafeBlocks` やネイティブ相互運用は不要。
- ビルド／発行は必ず DOTNET_ROOT を通す:
  ```sh
  DOTNET_ROOT=/opt/dotnet-ms PATH=/opt/dotnet-ms:$PATH dotnet build -c Release
  rm -rf bin obj Release
  DOTNET_ROOT=/opt/dotnet-ms PATH=/opt/dotnet-ms:$PATH dotnet publish -c Release -o Release
  ```
- **NuGet 依存なし**（.NET 8 / WinForms 標準ライブラリのみ）。
- 発行物は `Release/` に **`7-Zip-Auto.exe`（単一ファイル・自己完結）+ `LICENSE` + `THIRD-PARTY-NOTICES.txt`**。後者 2 つは csproj の `CopyLicenseFiles` ターゲット（`AfterTargets="Publish"`）が単一ファイル化の後にコピーする（`None ... CopyToPublishDirectory` は単一ファイル発行下では exe に取り込まれてしまうため不可）。
- アイコン `app.ico`（マルチ解像度）は csproj の `<ApplicationIcon>` + `<EmbeddedResource>`。生成は ImageMagick の **`magick`**（`convert` は IM7 で非推奨）。**oklch は ImageMagick 非対応**なので sRGB 値を自前計算して渡す。
- 実行・動作確認は Windows 側のみ。Windows 固有 API は Linux ビルドではエラーにならないが実行不可。ログは exe と同階層の `7-Zip-Auto.log`。
