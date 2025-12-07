# EasyNSIS 要件

## 1. プロジェクト概要

- アプリケーション名：`EasyNSIS`
- バージョン：`v1.0（初期版）`
- 開発目的：
  - NSIS の知識があまりなくても、簡単な GUI 操作だけでインストーラーを作成できるようにする
- ゴール：
  -「Program FilesやAppData 配下にインストールする、簡易インストーラーを 1 クリックで生成・ビルドできること」
- 想定利用ユーザー：
  - Windows デスクトップアプリを配布したいが、NSIS スクリプトを直接書きたくない開発者」

---

## 2. 対象プラットフォーム・開発環境

- 対象 OS：
  - `Windows 10 / 11 (x64)`
- 対象 アプリケーション:
  - `Windows 10 / 11 (x64)`
- 開発言語／フレームワーク：
  - `.NET 10 + WPF`
  - TFM: `net10.0-windows`（win-x64想定）
  - Publish: SelfContained + SingleFile（WPFはTrim非対応のためTrimは無効）
  - Fluentテーマを使用する
    - `<ResourceDictionary Source="pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml" />`を使用する
  - MVVM：CommunityToolkit.Mvvm,Microsoft.Xaml.Behaviors.Wpf
    - 最新版をnugetする
  - DI: Microsoft.Extensions.DependencyInjection
    - 最新版をnugetする

- NSIS 実行環境：
  - NSIS の想定バージョン：`NSIS 3.11`
  - 配布する NSIS は 32bit 版とし、`makensis.exe /VERSION` で 3.11 を検証。不一致・不在時は起動をブロックしガイド表示する。
  - 64bit インストーラー出力: 32bit NSIS で生成するが、`x64.nsh` / `SetRegView 64` / `$PROGRAMFILES64` 等を利用し 64bit OS/Program Files 配下向けに構成する。
  - NSIS のインストール前提：
    - このアプリケーションの実行ファイルと同じディレクトリにtoolsディレクトリを置いてそこに配置する。
        - toolsディレクトリのディレクトリ構成は以下
            ```
tools
└── nsis-3.11
    ├── Bin
    │   ├── GenPat.exe
    │   ├── MakeLangId.exe
    │   ├── RegTool-x86.bin
    │   ├── makensis.exe
    │   ├── zip2exe.exe
    │   └── zlib1.dll
    ├── COPYING
    ├── Contrib
    ├── Docs
    ├── Examples
    │   ├── AppGen.nsi
    │   ├── FileFunc.ini
    │   ├── FileFunc.nsi
    │   ├── FileFuncTest.nsi
    │   ├── Library.nsi
    │   ├── LogicLib.nsi
    │   ├── Memento.nsi
    │   ├── MultiUser.nsi
    │   ├── NSISMenu.nsi
    │   ├── StrFunc.nsi
    │   ├── TextFunc.ini
    │   ├── TextFunc.nsi
    │   ├── TextFuncTest.nsi
    │   ├── VersionInfo.nsi
    │   ├── WordFunc.ini
    │   ├── WordFunc.nsi
    │   ├── WordFuncTest.nsi
    │   ├── bigtest.nsi
    │   ├── example1.nsi
    │   ├── example2.nsi
    │   ├── gfx.nsi
    │   ├── install-per-user.nsi
    │   ├── install-shared.nsi
    │   ├── languages.nsi
    │   ├── makensis.nsi
    │   ├── one-section.nsi
    │   ├── primes.nsi
    │   ├── rtest.nsi
    │   ├── silent.nsi
    │   ├── unicode.nsi
    │   └── waplugin.nsi
    ├── Include
    │   ├── Colors.nsh
    │   ├── FileFunc.nsh
    │   ├── InstallOptions.nsh
    │   ├── Integration.nsh
    │   ├── LangFile.nsh
    │   ├── Library.nsh
    │   ├── LogicLib.nsh
    │   ├── MUI.nsh
    │   ├── MUI2.nsh
    │   ├── Memento.nsh
    │   ├── MultiUser.nsh
    │   ├── Sections.nsh
    │   ├── StrFunc.nsh
    │   ├── TextFunc.nsh
    │   ├── UpgradeDLL.nsh
    │   ├── Util.nsh
    │   ├── VB6RunTime.nsh
    │   ├── VPatchLib.nsh
    │   ├── WinCore.nsh
    │   ├── WinMessages.nsh
    │   ├── WinVer.nsh
    │   ├── WordFunc.nsh
    │   ├── nsDialogs.nsh
    │   └── x64.nsh
    ├── NSIS.chm
    ├── NSIS.exe
    ├── Plugins
    ├── Stubs
    │   ├── bzip2-x86-ansi
    │   ├── bzip2-x86-unicode
    │   ├── bzip2_solid-x86-ansi
    │   ├── bzip2_solid-x86-unicode
    │   ├── lzma-x86-ansi
    │   ├── lzma-x86-unicode
    │   ├── lzma_solid-x86-ansi
    │   ├── lzma_solid-x86-unicode
    │   ├── uninst
    │   ├── zlib-x86-ansi
    │   ├── zlib-x86-unicode
    │   ├── zlib_solid-x86-ansi
    │   └── zlib_solid-x86-unicode
    ├── makensis.exe
    ├── makensisw.exe
    └── nsisconf.nsh
            ```
- ビルド・配布形態：
  - 作成したアプリケーションとともにtoolsにNSISを配置して、zipで圧縮して配布します。

---

## 3. スコープ（v1.0 で対応する範囲）

- v1.0 で **必須対応**すること：
  - NSIS スクリプトの自動生成
  - NSIS コンパイラ（makensis）の呼び出し
  - 多言語インストーラー
    - 日本語と英語に対応
    - インストーラーの基本的なもの（デフォルトで表示されるボタンやタイトルバーの文字列）を指定
    - 言語はロケールもしくは、固定で指定

---

## 4. 基本機能要件（インストーラーの仕様）

### 4.1 基本情報の指定
- 会社名もしくは所属団体
  - 指定方法：テキストボックス
- アプリケーション名
  - 指定方法：テキストボックス
- バージョン
  - 指定方法：テキストボックス
- 言語
  - 指定方法: ラジオボタンでロケール使用と固定言語、固定言語の場合、コンボボックスで言語を指定
- 登録モード
  - 指定方法: ラジオボタンで以下の2つから選択（排他設定）
    - アプリと機能に登録する（デフォルト）
    - レジストリを使用しない（「アプリと機能への登録が行われず、一部機能が無効になります」と画面内に注意書きを表示）
- インストーラーのアイコン設定
    - ファイル選択ダイアログ
    - テキスト入力
    - 設定されない場合は、デフォルトのアイコンを使用する
    - アイコン取得に失敗した場合はアイコンを設定しない（NSISデフォルト）
  - 言語選択の初期値: OSロケール優先（ja-JPなら日本語、それ以外は英語）
  - ライセンス種別の初期値: text（画面内入力）

### 4.2 インストール対象フォルダー

- インストールパッケージに含めるフォルダー：
  - 元フォルダーのパス指定方法：
    - フォルダー選択ダイアログ
    - テキスト入力
  - サブフォルダー再帰含め：
    - 必須：`サブフォルダーを含めてパッケージに含める`
  - 指定したフォルダーをルートフォルダーとする
  - ファイル・ディレクトリ除外機能はv1.0では対応しない
  - 予約ファイル名のエラー：インストール先ルートで使用する `installed_files.dat`、`installed_version.dat`、`Uninstall.exe` と同名のファイルがインストール元に存在する場合は、ビルド時にエラーとして停止する（これらの予約ファイル名はソースフォルダーに含めることができない）。

### 4.3 インストール先の既定パス

- 既定インストール先：
  - ラジオボタンを縦に並べて以下を選択する
    - AppData（ローミング）:`$APPDATA\<会社名もしくは所属団体>\<アプリ名>`  
    - AppData（ローカル）:`$LOCALAPPDATA\<会社名もしくは所属団体>\<アプリ名>`  
    - Program Files :`$PROGRAMFILES\<会社名もしくは所属団体>\<アプリ名>`  
      - 選択された場合は、管理者権限要求を行う
      - 64bit限定

   - その他（任意指定）：`__________________________________`
- ユーザーによる変更可否：
  - インストール先をユーザーが変更できる
  - 既定選択: 「AppData（ローミング）」

### 4.4 ショートカット作成

- 作成するショートカット：
  - デスクトップへのショートカット
    - 「+ファイル」、「+フォルダ」ボタンでファイル、フォルダーを作成するショートカットに追加する
      - 指定方法: ファイル選択ダイアログ、フォルダー選択ダイアログ
    - 追加されたファイル、フォルダーのパスをListViewに表示する
    - ListViewでパスを選択「-」で追加されたファイル、フォルダーを削除する
    - ショートカットのパラメータはv1.0では対応しない

  - スタートメニューへのショートカット
    - スタートメニューに`<会社名もしくは所属団体>`フォルダーを作成し、その下にショートカットを作成する
    - 「+ファイル」、「+フォルダ」ボタンでファイル、フォルダーを作成するショートカットに追加する
      - 指定方法: ファイル選択ダイアログ、フォルダー選択ダイアログ
    - 追加されたファイル、フォルダーのパスをListViewに表示する
    - ListViewでパスを選択「-」で追加されたファイル、フォルダーを削除する

### 4.5 使用許諾書（ライセンス）表示

- 表示するタイミング：
  - インストール開始前のページで表示（同意しないと先に進めない）
- ライセンステキストの入力方法：
  - 外部テキストファイルを指定（例：`license.txt`）
    - フォルダー選択ダイアログ
    - テキスト入力
  - EasyNSIS 画面内に直接テキスト入力
  - ライセンスファイルが指定されていない場合は、アプリケーションのデフォルトのライセンスファイルを使用する。
    - アプリケーションの実行ファイルと同じディレクトリにassetsディレクトリを作成し、デフォルトのライセンスファイルを作成して配置する
      - 日本語版と英語版を作成する
      - 内容は、BtoBで一般的な「商用利用可・再配布不可・無保証・責任制限」を含む使用許諾内容を記載する

- 対応文字コード：
  - UTF-8

### 4.6 アンインストール
- Uninstallを行う実行ファイルの作成
  - アプリと機能に登録している場合は、登録を削除する
  - 自身のアプリケーションフォルダーのルートに配置する
- スタートメニューにUninstallを行う実行ファイルのショートカットを作成する
- クリーンアップ範囲を選択できる
  - すべて削除: インストール先配下のファイルをすべて削除する
  - ユーザーデータ保護: インストール時に配置したファイルのみ削除し、ユーザーが後から追加したファイルは残す
  - UI: ラジオボタンで選択（デフォルトは「ユーザーデータ保護」）
- 削除ガード
  - アンインストール時は `$INSTDIR` 配下のファイルを削除する。`$INSTDIR` 自体が存在しない場合やアクセス権がない場合は警告を表示する。
- アップグレード時の動作
  - 既存インストールを検出し、バージョンを比較する
  - 同じまたは古いバージョンがインストール済みの場合は、ユーザーに通知して処理を中断する
  - 新しいバージョンの場合は、ユーザーデータ保護のクリーンアップを実行した上で、再インストールする
- 登録モードが「アプリと機能に登録する」の場合、アプリと機能からもアンインストール可能
- Program Filesにインストールした場合、HKLMを使用し、AppDataはHKCUを使用する
- 書き込む項目（DisplayName/DisplayVersion/Publisher/DisplayIcon/InstallLocation/UninstallString等）は、基本情報から取得する
- 登録モードが「レジストリを使用しない」の場合は、アプリと機能への登録・レジストリ書き込みを行わない（インストーラー/アンインストーラーはファイルベースのみ）。
  - レジストリを使用しない場合もアンインストーラーは生成し、インストールしたファイル削除後に自身（Uninstall.exe）も削除する。

### 4.7 出力インストーラー
- 指定したアプリケーション名を使用する
- lzma

### 4.8 作成したインストーラー出力フォルダー
- フォルダーのパス指定方法：
  - フォルダー選択ダイアログ
  - テキスト入力
  - 初期値: ユーザーの「ドキュメント」フォルダー（My Documents/`%USERPROFILE%\Documents`）

### 4.9 インストール後設定
- インストールフォルダーを開く
  - 指定方法: チェックボックス
  - デフォルト: オフ
  - インストール完了後にエクスプローラーでインストール先フォルダーを開く
- インストール後に実行するファイル
  - 指定方法: チェックボックス、テキスト入力、ファイル選択ダイアログ
  - デフォルト: オフ
  - インストール対象フォルダー内のバッチファイル（.bat, .cmd）または実行ファイル（.exe）を指定可能
  - インストール完了後に指定したファイルを実行する

### 4.10 ログ・エラー対応
- 例外やエラーが発生した場合は、MessageBoxに表示を行う
- 詳細なログをアプリケーションの実行ファイルと同じディレクトリにError.logファイルを追記・作成を行う
  - 記載内容は日時(ISO8601), エラー内容とする
  - UTF-8、最大 1000 行でローテーション。1000 行到達時に `.1` へリネームして新規作成（過去の `.1` は上書き）。
- ビルドログは、実行ファイルと同じディレクトリにNSIS_Build.logファイルを追記・作成を行う
  - 記載内容は日時(ISO8601), ビルドログ内容とする
  - UTF-8、最大 1000 行でローテーション。1000 行到達時に `.1` へリネームして新規作成（過去の `.1` は上書き）。


### 4.11 設定保存
- インストーラーの設定を保存・復元する
  - ファイル選択ダイアログ
  - テキスト入力
  - 保存・復元（読み込み）ボタン
  - XML

### 4.12 ビルド実行時
- 記載内容が足りない場合は、MessageBoxに表示を行う
  - アンインストーラー作成や、アプリと機能に登録、レジストリ書き込みのための必要なものをチェックする

---

### 5.1 画面構成（UI）仕様

#### 5.1.1 メイン画面

- レイアウトイメージ：
  - 左側：設定項目のタブ／リスト
  - 右側：選択された項目の編集エリア
  - 下部：NSIS ビルドボタン・ステータスログ
    - ステータスログは、ビルドログをリアルタイムに表示し、最後に、成功・失敗を表示する

- 左側タブ：
- 基本情報
- インストール対象フォルダー設定
- インストール先設定
- ショートカット設定
- ライセンス設定
- 出力設定
- インストール後設定
- 設定保存・復元

---

## 6. 補足仕様とデフォルト方針

- ビルド/配布前提
  - アプリは `.NET 10 WPF` で win-x64のみ、自己完結（self-contained）、単一ファイル配布（assetsフォルダーは別）。
  - 配布物は publish 出力 + `tools/nsis-3.11` を同一階層に置き、zip 配布する。
  - 起動時に `tools/nsis-3.11/makensis.exe` の存在と `3.11` バージョンを確認し、不足・異なる場合はブロックしてガイド表示する。

- セキュリティ/配布品質：
  - デジタル署名は行わない

- 自動化/CI：
  - CLIは用意しない

- ローカライズ
  - サポート言語は `ja-JP` と `en-US`。OS ロケールが `ja-JP` のとき日本語、その他は `en-US` にフォールバック。固定言語指定時はこの2言語のみ選択肢。
  - UI テキストは resx で管理し、インストーラーの基本文字列は NSIS 標準の日本語/英語言語テーブルを使用し、必要なカスタム文字列のみ上書きする。
  - デフォルトライセンスは `assets/license-ja.txt` と `assets/license-en.txt` に配置し、BtoBで一般的な「商用利用可・再配布不可・無保証・責任制限」を含む文面を記載する。外部ファイル指定があればそれを優先する。

- 入力/バリデーション
  - 必須入力: 会社名/所属団体、アプリケーション名、バージョン、インストール対象フォルダー、出力フォルダー。
  - バージョン書式: 数字とドットのみ（最大4セグメント、例 `1.0.0`）。不正ならエラー。
  - 会社名/アプリ名はパスに使用するため `<>:"/\\|?*` を含めない。各64文字以内。前後空白はトリムし、先頭末尾のドット/スペースは禁止。
  - インストーラーアイコンは `.ico` ファイルのみ許可し、存在チェック。未指定/取得失敗時はアイコンを設定しない（NSISデフォルト）。
  - インストール対象フォルダーは存在チェックを行い、シンボリックリンクは除外して警告する。隠しファイルも含めて再帰コピーする。
  - 設定値の整合性:
    - 「登録モード」は排他設定であり、`RegisterToAppsAndFeatures`（アプリと機能に登録する）または `NoRegistry`（レジストリを使用しない）のいずれか1つが選択される。
  - パスの検証（インストール先/出力先/各入力）
    - 前後空白はトリム後に検証。末尾のドット/スペースは不可。
    - 禁止文字: `<>:"/\\|?*`。UNC/ネットワークパスは不可。
    - カスタムパスはローカル絶対パスまたは環境変数展開形式（`%USERPROFILE%\\App\\Foo` など）のみ許可。環境変数未解決・解決結果が無効な場合はエラー。
    - 正規化後にルートなしや相対・`..` が混在する場合はエラー。環境変数展開結果も同様に検証。

- インストール先/権限
  - 既定選択は「AppData（ローミング）」。Program Files 選択時は管理者権限を要求する（昇格できない場合はビルドをブロック）。
  - 「その他」はローカル絶対パスまたは環境変数展開形式（`%USERPROFILE%\\App\\Foo` など）を許可。UNC/ネットワークパスは不可。禁止文字は上記と同じ。
  - ユーザーが変更したパスも同じルールで検証し、不正・存在不可ならエラー表示。

- ショートカット
  - 追加できるのは存在するファイル/フォルダーのみ。フォルダーを指定した場合はエクスプローラーでそのフォルダーを開くショートカットを作成。
  - ショートカット名はターゲットのファイル/フォルダー名をデフォルトとし、アイコンはターゲットから取得（取得不可ならインストーラーアイコン）。
  - スタートメニューのルートフォルダー名は「会社名/所属団体」。未入力時はアプリ名で代替。

- アンインストール/レジストリ

  - クリーンアップ範囲はユーザー選択に従う（デフォルトはユーザーデータ保護）。

  - インストール時は、まず `$INSTDIR`（インストール先フォルダー）の存在を確認する。

    - **新規インストール（`$INSTDIR` が存在しない場合）:**

      - バージョン確認は不要。そのまま新規インストールとして続行する。

    - **既存インストールの検出（`$INSTDIR` が存在する場合）:**

      - 既存インストールがあると判断し、バージョン確認を実行する。

    - **バージョン確認の優先順位:**

      1. まず `$INSTDIR\installed_version.dat` ファイルの存在を確認し、存在すればそのファイルからバージョン情報を読み取る。

      2. 登録モードが「レジストリを使用しない」以外の場合のみ、上記ファイルが存在しない場合にフォールバックとしてレジストリ (`Software\Microsoft\Windows\CurrentVersion\Uninstall\<アプリ名>` の `DisplayVersion` 値) からバージョン情報を読み取ることを試みる。

    - **バージョン比較:**

      - 読み取ったインストール済みバージョンと、新しいインストーラーのバージョンを比較する。

      - 比較ロジック:

        - 4桁に足りない場合は4桁に拡張する（例: `1.0` → `1.0.0.0`）

        - 各セグメントを整数として左から順に比較する（例: `1.9` < `1.10`）

      - 新しいバージョンの方が大きい場合: ユーザーデータ保護モードでアンインストールを実行後、再インストールする。

      - 同じか、または古いバージョンがインストール済みの場合: 「同じか、より古いバージョンがインストール済みです。」と通知し、インストールを中断する。

      - `$INSTDIR` は存在するがバージョンが取得できない場合（`installed_version.dat` が欠如/パース不能、かつレジストリからも取得できない場合）: 「インストール済みバージョンを判定できません。続行できません。」と通知し、中断する。この場合、レジストリは削除し（残存させない）、中断メッセージを表示してインストール先フォルダーを開く（ユーザー自身で手動削除を促す）。

  - バージョン情報ファイル `$INSTDIR\installed_version.dat` は、インストーラーによってUTF-8（1行にバージョン文字列）で保存・参照される。

  - インストール元に `installed_version.dat` と同名ファイルが存在する場合はビルド時にエラーとして停止する。

  - ユーザーデータ保護のため、インストール時に配置したファイルの一覧を `$INSTDIR` 直下（例: `$INSTDIR\installed_files.dat`）に保存し、アンインストール時はこのリストを基に削除対象を決定する。

    - 文字コード: UTF-8（BOMなし）。

    - 書式: 1行1パス、`$INSTDIR` からの相対パスで保存（先頭に `./` は付けない）。

    - アンインストール時は相対パスを `$INSTDIR` に連結して削除対象を特定する。`..` を含む行はスキップして警告。

    - `installed_files.dat` は EasyNSIS が生成・管理し、インストール元に同名ファイルがある場合はビルド時にエラーとして停止する。

  - アンインストール時は `$INSTDIR` 配下のファイルを削除する。`$INSTDIR` が存在しない場合やアクセス権がない場合は警告を表示する。

  - ロック中のファイルはスキップして警告表示。ショートカットとレジストリエントリはクリーンアップを試行。

  - 登録モードが「アプリと機能に登録する」の場合、レジストリは標準的な構造に従う。

    - `InstallDirRegKey` は `Software\<会社名or組織名>\<アプリ名>` を使用する。

    - アンインストール情報を `Software\Microsoft\Windows\CurrentVersion\Uninstall\<アプリ名>` の配下に登録し、`Publisher` 値に会社名/組織名を設定する。

  - Program Files インストール時は HKLM の 64bit ビュー、AppData 時は HKCU を使用する。

  - 登録モードが「レジストリを使用しない」の場合はレジストリ書き込み・アプリと機能登録をスキップする。
  - アンインストーラーはレジストリ使用有無に関わらず、ファイル削除完了後に自削除する。自削除はNSISで一般的に用いられる手法（例: `Delete /REBOOTOK "$EXEPATH"` または終了後にcmd.exeで遅延削除を実行）を採用する。



- ビルド出力/ログ

  - 出力インストーラー名は `<アプリ名>_<バージョン>_Setup.exe`（バージョン未入力時は `<アプリ名>_Setup.exe`）。圧縮は lzma 固定。

  - 出力フォルダーが存在しない場合は作成を試み、不可ならエラーで中断。

  - `Error.log` / `NSIS_Build.log` は UTF-8、最大 1000 行でローテーション。1000 行到達時に `.1` へリネームして新規作成（過去の `.1` は上書き）。



- 設定保存（XML）

  - ルート要素は `EasyNsisConfig`、属性 `version="1"` を持つ。パスは基本的に絶対パスで保存し、相対パスが含まれていた場合は設定ファイルの場所からの相対とみなす。

  - 全入力項目（基本情報・インストール対象フォルダー・インストール先・ショートカット・ライセンス設定・アンインストール設定・出力設定・登録モード・クリーンアップ範囲・保存先など）を保存する。

  - 不正な XML や欠落項目は読み込み時にエラー表示し、既存設定を変更しない。



- ビルド前チェック

  - 必須入力の欠落、インストール対象フォルダー不存在、出力フォルダー作成失敗、アイコン未存在、NSIS 不在/バージョン不一致、Program Files 選択時の非昇格はビルドをブロックし MessageBox で通知する。



- build/publish

  - assetsフォルダーをビルド、パブリッシュ時に出力先にコピーする

  - toolsフォルダーをビルド、パブリッシュ時に出力先にコピーする

  - ビルド、パブリッシュする前に、開発者によってプロジェクトのフォルダーにtools、tools/nsis-3.11を配置します。

  - Publish は SelfContained + SingleFile で行う（WPFはTrim非対応のためTrimは無効）。

  - 単一ファイル publish でも実行ファイルは `AppContext.BaseDirectory`（publish 出力先）を基準に、同じディレクトリの `tools` と `assets` を参照する。サイドロード先に `tools/nsis-3.11` と `assets` が存在しない場合は起動時にエラーとする。



- 多重起動防止

  - アプリ起動時に単一インスタンスを確認し、2重起動をブロックする（ユーザーに通知する）。



---



## 7. UI/UX 詳細仕様



### 7.1 ウィンドウ仕様

- 初期サイズ: 1100 x 768 ピクセル

- 最小サイズ: 900 x 600 ピクセル

- リサイズ: 可能

- 起動位置: 画面中央

- タイトルバー: 「EasyNSIS - <設定ファイル名>」（未保存時は「EasyNSIS - 新規」）



### 7.2 左側タブパネル

- 幅: 240px（固定）

- タブ項目:

  1. 基本情報

  2. インストール対象フォルダー

  3. インストール先

  4. ショートカット

  5. ライセンス

  6. 出力設定

  7. インストール後設定

  8. 設定保存・復元



### 7.3 右側編集エリア

- 各タブに対応した設定フォームを表示

- スクロール: 内容がエリアを超える場合は垂直スクロールバーを表示



### 7.4 下部ステータスエリア

- 高さ: 180px（初期値、ドラッグで調整可能）

- 構成:

  - ビルドボタン（右寄せ）

  - ステータスログ表示エリア（TextBox、読み取り専用、等幅フォント）

- ビルド中の状態:

  - ビルドボタン: 「中断」に文字列が変化し、クリックするとビルドを中断する。ビルド終了後は「ビルド」に戻る。

  - 左側タブパネル: 無効化

  - 右側編集エリア: 無効化

  - ログエリア: makensisの出力をリアルタイム表示



### 7.5 アプリケーション終了時の動作

- 未保存の変更がある場合: 確認ダイアログを表示

  - 「保存して終了」「保存せずに終了」「キャンセル」の3択

  - 未保存の検出方法: 最後の保存時点との差分で判定する。設定を保存した時点の値を記憶し、現在の値と比較して差異があれば「未保存」と判定する。新規作成時は初期値を「保存済み」として扱う。保存後に変更して元に戻した場合は「保存済み」と判定する。

- ビルド中の場合: 終了をブロックし、ビルド完了を待つか中断するか確認



---



## 8. NSISスクリプト生成仕様



### 8.1 生成スクリプトの基本構造

```nsi

; EasyNSIS Generated Script

; Generated: <生成日時 ISO8601>



!include "MUI2.nsh"

!include "FileFunc.nsh"

!include "x64.nsh"



; 基本設定

Name "<アプリケーション名>"

OutFile "<出力ファイル名>"

InstallDir "<既定インストール先>"

; レジストリ使用時のみ: InstallDirRegKey <HKLM|HKCU> "Software\<会社名>\<アプリ名>" "InstallLocation"

RequestExecutionLevel <admin|user>

SetCompressor /SOLID lzma

Unicode True



; バージョン情報

VIProductVersion "<バージョン.0>" ; 4セグメント形式（入力が4セグメント未満なら末尾に0を補って4セグメント化）

VIAddVersionKey "ProductName" "<アプリケーション名>"

VIAddVersionKey "CompanyName" "<会社名>"

VIAddVersionKey "ProductVersion" "<バージョン>"

VIAddVersionKey "FileVersion" "<バージョン>"

VIAddVersionKey "FileDescription" "<アプリケーション名> Installer"

VIAddVersionKey "LegalCopyright" "© <会社名>"



; アイコン設定

!define MUI_ICON "<アイコンパス>"

!define MUI_UNICON "<アイコンパス>"



; 言語設定

!insertmacro MUI_PAGE_WELCOME

!insertmacro MUI_PAGE_LICENSE "<ライセンスファイル>"

!insertmacro MUI_PAGE_DIRECTORY

!insertmacro MUI_PAGE_INSTFILES

!insertmacro MUI_PAGE_FINISH



!insertmacro MUI_UNPAGE_CONFIRM

!insertmacro MUI_UNPAGE_INSTFILES



!insertmacro MUI_LANGUAGE "Japanese"

!insertmacro MUI_LANGUAGE "English"



; インストールセクション

Section "Install"

    SetOutPath "$INSTDIR"



    ; 既存インストールのバージョン確認

    ;   - 同じ/古い場合はメッセージ表示して中断

    ;   - 新しい場合はユーザーデータ保護で一度アンインストール



    ; ファイルコピー

    File /r "<インストール対象フォルダー>\*.*"



    ; 配置したファイルのリストを保存（例: $INSTDIR\installed_files.dat）

    ;   ユーザーデータ保護アンインストール時はこのリストを参照して削除する



    ; アンインストーラー作成

    WriteUninstaller "$INSTDIR\Uninstall.exe"



    ; ショートカット作成

    ; （デスクトップ、スタートメニュー）



    ; レジストリ登録（アプリと機能）

SectionEnd



; アンインストールセクション（レジストリを使用しない場合はレジストリ削除部分を生成しない）

Section "Uninstall"

    ; ファイル削除（クリーンアップ範囲は設定による）

    ;   削除対象パスは正規化し、$INSTDIR 先頭一致のみ許可。ずれた場合はスキップして警告。

    ; ショートカット削除

    ; レジストリ削除

SectionEnd

```



### 8.2 使用するNSISディレクティブ/マクロ

- `MUI2.nsh`: Modern UI 2 （ウィザード形式UI）

- `FileFunc.nsh`: ファイル操作関数

- `x64.nsh`: 64bit環境判定

- `SetCompressor /SOLID lzma`: LZMA圧縮

- `Unicode True`: Unicode対応



### 8.3 中間ファイル

- 生成されたNSISスクリプトは出力フォルダーに `<アプリ名>_Setup.nsi` として保存

- ビルド失敗時も削除しない（デバッグ用途）



---



## 9. エラーハンドリング詳細



### 9.1 makensis実行時のエラー処理

| 終了コード | 意味 | 対処 |

|-----------|------|------|

| 0 | 成功 | 成功メッセージを表示 |

| 1 | 一般エラー | エラーログを表示、ビルドログに詳細記録 |

| 2 | 構文エラー | スクリプト生成のバグとして詳細をログに記録 |



### 9.2 ビルド中断時の処理

- ユーザーによる中断: makensisプロセスを強制終了

- 中間ファイル（.nsiスクリプト）は削除しない

- ログに「ユーザーによって中断されました」と記録



### 9.3 例外発生時の処理

- すべての未処理例外をキャッチ

- MessageBoxでユーザーに通知

- Error.logに詳細（スタックトレース含む）を記録

- アプリケーションは可能な限り継続動作



---



## 10. バリデーション仕様



### 10.1 バリデーションタイミング

- **リアルタイムバリデーション**:

  - フォーカス移動時に該当フィールドを検証

  - エラー時はフィールド横に赤色でエラーメッセージ表示

- **ビルド時バリデーション**:

  - ビルドボタン押下時に全項目を再検証

  - エラーがある場合はMessageBoxで一覧表示し、最初のエラー項目のタブに移動



### 10.2 エラー表示形式

- フィールド単位: テキストボックスの境界線を赤色に変更、下部に赤文字でエラーメッセージ

- ビルド時一括: MessageBoxに箇条書きで全エラーを表示



---



## 11. 設定ファイル（XML）スキーマ



### 11.1 XML構造

```xml

<?xml version="1.0" encoding="utf-8"?>

<EasyNsisConfig version="1">

  <BasicInfo>

    <CompanyName>会社名</CompanyName>

    <ApplicationName>アプリ名</ApplicationName>

    <Version>1.0.0</Version>

    <Language type="locale|fixed" value="ja-JP" />

    <RegistrationMode>RegisterToAppsAndFeatures|NoRegistry</RegistrationMode>

    <InstallerIcon>C:\path\to\icon.ico</InstallerIcon>

  </BasicInfo>



  <SourceFolder>

    <Path>C:\path\to\source</Path>

  </SourceFolder>



  <InstallDestination>

    <Type>AppDataRoaming|AppDataLocal|ProgramFiles|Custom</Type>

    <CustomPath></CustomPath>

    <AllowUserChange>true</AllowUserChange>

  </InstallDestination>



  <Shortcuts>

    <Desktop>

      <Item type="file">relative\path\to\app.exe</Item>

      <Item type="folder">relative\path\to\docs</Item>

    </Desktop>

    <StartMenu>

      <Item type="file">relative\path\to\app.exe</Item>

    </StartMenu>

  </Shortcuts>



  <License>

    <Type>default|file|text</Type>

    <FilePath></FilePath>

    <Text></Text>

  </License>



  <Uninstall>

    <Cleanup>All|PreserveUserData</Cleanup>

  </Uninstall>



  <Output>

    <Folder>C:\path\to\output</Folder>

  </Output>



  <PostInstall>

    <OpenInstallFolder>false</OpenInstallFolder>

    <RunAfterInstall>false</RunAfterInstall>

    <RunAfterInstallPath>relative\path\to\app.exe</RunAfterInstallPath>

  </PostInstall>

</EasyNsisConfig>

```



### 11.2 パスの扱い

- 絶対パス: そのまま保存

- 相対パス: 設定ファイルの保存場所を基準に解決



### 11.3 読み込みエラー時の動作

- XMLパースエラー: MessageBoxでエラー表示、現在の設定を維持

- 必須項目欠落: 該当項目のみデフォルト値で補完、警告表示

- 不明な要素: 無視して読み込み続行



---



## 12. 多言語対応詳細



### 12.1 resxファイル構成

- `Resources/Strings.resx`: デフォルト（英語）

- `Resources/Strings.ja.resx`: 日本語



### 12.2 主要なリソースキー

| キー | 英語 | 日本語 |

|-----|------|--------|

| `AppTitle` | EasyNSIS | EasyNSIS |

| `Tab_BasicInfo` | Basic Information | 基本情報 |

| `Tab_SourceFolder` | Source Folder | インストール対象フォルダー |

| `Tab_InstallDestination` | Install Destination | インストール先 |

| `Tab_Shortcuts` | Shortcuts | ショートカット |

| `Tab_License` | License | ライセンス |

| `Tab_Output` | Output Settings | 出力設定 |

| `Tab_SaveLoad` | Save/Load | 設定保存・復元 |

| `Btn_Build` | Build Installer | インストーラー作成 |

| `Btn_Browse` | Browse... | 参照... |

| `Msg_BuildSuccess` | Build completed successfully. | ビルドが正常に完了しました。 |

| `Msg_BuildFailed` | Build failed. See log for details. | ビルドに失敗しました。ログを確認してください。 |

| `Msg_UnsavedChanges` | There are unsaved changes. Do you want to save before exiting? | 未保存の変更があります。終了前に保存しますか？ |

| `Err_RequiredField` | This field is required. | この項目は必須です。 |

| `Err_InvalidVersion` | Version must be in format: X.X.X (max 4 segments) | バージョンは X.X.X 形式で入力してください（最大4セグメント） |

| `Err_InvalidPath` | Path contains invalid characters. | パスに使用できない文字が含まれています。 |



### 12.3 NSIS言語テーブル

- 標準のNSIS日本語/英語言語ファイルを使用

- カスタマイズが必要な文字列は `LangString` で上書き



---



## 13. ショートカット詳細仕様



### 13.1 指定可能なファイル/フォルダー

- インストール対象フォルダー内のファイル/フォルダーのみ指定可能

- 外部パスは指定不可（インストーラーに含まれないため）



### 13.2 パスの扱い

- 設定ファイルには相対パス（インストール対象フォルダーからの相対）で保存

- インストーラー内では `$INSTDIR` からの相対パスに変換



### 13.3 ショートカット名

- ファイル: 拡張子を除いたファイル名

- フォルダー: フォルダー名

- 重複時: 自動的に連番を付与（例: `App`, `App (2)`）



---



## 14. ビルド完了時の動作



### 14.1 成功時

- ステータスログに「ビルドが正常に完了しました。」と表示

- MessageBoxの様なダイアログで成功を通知

  - 「OK」ボタンを表示、クリックでダイアログを閉じる

  - 「出力フォルダーを開く」ボタンを表示（クリックでエクスプローラーを開く）



### 14.2 失敗時

- ステータスログにエラー内容を表示

- MessageBoxで失敗を通知

- NSISスクリプト（.nsi）は削除せず残す（デバッグ用）
