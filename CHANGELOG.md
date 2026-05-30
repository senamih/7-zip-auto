# Changelog

このファイルは本プロジェクトの主要な変更点を記録します。
書式は [Keep a Changelog](https://keepachangelog.com/ja/1.1.0/) に準拠し、
バージョニングは [Semantic Versioning](https://semver.org/lang/ja/) に従います。

## [Unreleased]

## [1.2.1] - 2026-05-30

### Fixed
- アーカイブ末尾に余分なデータが付いた zip（Google Fonts のダウンロード zip 等）で、ファイルは正常に展開できているのに 7zG.exe が終了コード 2（「There are data after the end of archive」相当の無害な警告）を返し、一覧で「失敗」表示になっていた問題を修正。7zG.exe が終了コード 2 のときはコンソール版 7z でアーカイブ実体を検証し、健全であれば「完了」として扱う。終了コード 1（警告）も完了扱いとする。本当に破損している場合・ユーザ中断（255）等は従来どおり失敗のまま。

## [1.2.0] - 2026-05-29

### Added
- `README.md` に免責事項（無保証・損害について作者は責任を負わない旨）を追記。
- 発行物にランタイム非同梱（framework-dependent）版 `7-Zip-Auto-fd.exe`（約 1.4 MB、別途 .NET 10 デスクトップランタイムが必要）を追加。自己完結版 `7-Zip-Auto.exe`（約 52 MB、ランタイム不要）と併せて 2 種類を Release に同梱。`README.md` に両者の使い分けを明記。

### Changed
- ターゲットフレームワークを .NET 8 から **.NET 10**（`net10.0-windows`）へ更新。あわせて .NET 10 の WinForms アナライザ(WFO1000)対応として、`MainForm` の実行時専用プロパティに `[DesignerSerializationVisibility(Hidden)]` を付与。

### Fixed
- 展開先フォルダ名（アーカイブ名から拡張子を除いた名前）が同階層の既存ファイルと衝突する場合（例: `test.pptx.zip` の展開先 `test.pptx` と元ファイル `test.pptx`）に「同名のファイル／ディレクトリが既に存在する」で展開失敗していた問題を修正。衝突時は ` (2)`, ` (3)` … と連番を付けた未使用フォルダへ展開する（既存フォルダがある場合は従来どおりそこへ展開）。

## [1.1.0] - 2026-05-18

### Added
- 設定画面の左下にソフト名＋バージョン（`AppInfo.TitleWithVersion`）のリンクを配置し、クリックで GitHub リポジトリを既定ブラウザで開くようにした。

## [1.0.0] - 2026-05-18

初回リリース。

### Added
- 圧縮ファイルを引数／ドラッグ＆ドロップ／拡張子の関連付けで受け取り、7zG.exe で自動展開する基本機能。
- 展開中の項目を行表示する一覧 UI（ファイル名／サイズ／エントリ数／状態／フォルダを開く・削除ボタン）。
- 多重起動防止と展開キュー管理（複数投入時は順次処理、2 件以上で自動表示）。
- タスクトレイ常駐 ON/OFF、無人実行（SilentMode）、一覧空時の自動クローズ等の起動・常駐制御。
- 設定画面（7zG.exe／ファイラ指定、各種自動削除スイッチ、拡張子の関連付け）と `settings.json` への保存。
- 7-Zip 未検出時の導線集約ガイドダイアログ `SevenZipGuideForm`。公式サイト誘導／winget インストール／再検出／手動指定／今はしない を 1 ダイアログに集約。
- `SevenZipFinder` の検出経路に既定インストール先パス探索（`%ProgramW6432%`／`%ProgramFiles%`／`%ProgramFiles(x86)%`\7-Zip\7zG.exe）を追加。
- デバッグ用起動スイッチ `--test-guide`：7-Zip 未検出を再現して通常起動する（通常ウィンドウを開いたうえで実経路と同じガイドを表示し、閉じても画面が残る。settings.json は改変しない）。
- ウィンドウタイトルにバージョンを併記（例：`7-Zip-Auto v1.0.0`。`AppInfo` ヘルパ追加）。
- 同梱ライセンス（`LICENSE` / `THIRD-PARTY-NOTICES.txt`）と発行先への自動コピー。
- エンドユーザ向け `README.md`（対応書式・常駐／無人動作・設定/ログ/アンインストール・FAQ・変更履歴の節）、`CHANGELOG.md` を発行物（`Release/`）に同梱。
- 簡易ロガー（`7-Zip-Auto.log`）、専用アプリアイコン。
- 単一ファイル自己完結発行（`Release/7-Zip-Auto.exe`）。

### Changed
- 7zG.exe 未検出時の挙動：警告のみの行き止まり → ガイドダイアログへ集約。投入済みファイルは「待機中」で一覧に保持し、パス確定後に自動で展開を再開する。
- ガイドダイアログの「再検出」成功時に、検出したパスを通知ダイアログで表示してから通常動作へ遷移するようにした。
- ソースファイルを `Src/` に整理。

[Unreleased]: https://github.com/senamih/7-zip-auto/compare/v1.2.1...HEAD
[1.2.1]: https://github.com/senamih/7-zip-auto/compare/v1.2.0...v1.2.1
[1.2.0]: https://github.com/senamih/7-zip-auto/compare/v1.1.0...v1.2.0
[1.1.0]: https://github.com/senamih/7-zip-auto/compare/v1.0.0...v1.1.0
[1.0.0]: https://github.com/senamih/7-zip-auto/releases/tag/v1.0.0
