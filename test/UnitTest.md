# EasyNSIS Unit Tests

EasyNSISプロジェクトのユニットテストに関するドキュメントです。

## テスト環境

| 項目 | 値 |
|------|-----|
| テストフレームワーク | xUnit 2.9.3 |
| ターゲットフレームワーク | .NET 10.0 |
| カバレッジツール | coverlet.collector 6.0.4 |

## プロジェクト構成

```
tests/
└── EasyNSIS.Tests/
    ├── EasyNSIS.Tests.csproj
    ├── Services/
    │   ├── ValidationServiceTests.cs
    │   ├── ConfigurationServiceTests.cs
    │   └── NsisServiceTests.cs
    └── Helpers/
        └── ConverterTests.cs
```

## テスト実行方法

### コマンドラインから実行

```bash
# テスト実行
dotnet test tests/EasyNSIS.Tests

# 詳細出力付き
dotnet test tests/EasyNSIS.Tests --verbosity normal

# 特定のテストのみ実行
dotnet test tests/EasyNSIS.Tests --filter "FullyQualifiedName~ValidationService"
```

### Visual Studioから実行

1. テストエクスプローラーを開く（テスト → テストエクスプローラー）
2. 「すべてのテストを実行」をクリック

## テストカテゴリ

### ValidationService Tests (64テスト)

入力値のバリデーションロジックをテストします。

| テストID | テスト内容 |
|----------|-----------|
| UT-VAL-001〜010 | 会社名バリデーション（正常値、空文字（オプション）、null（オプション）、文字数制限、禁止文字、日本語対応） |
| UT-VAL-011〜019 | アプリケーション名バリデーション |
| UT-VAL-020〜029 | バージョン番号バリデーション（フォーマット、セグメント数） |
| UT-VAL-030〜036 | パスバリデーション（絶対パス、UNC禁止、禁止文字） |
| UT-VAL-040〜044 | アイコンパスバリデーション（.ico形式、存在確認） |
| UT-VAL-050〜053 | ソースフォルダー予約ファイルチェック |
| UT-VAL-060〜066 | カスタムインストールパスバリデーション（UNC禁止、パストラバーサル禁止、ドライブルート禁止、NSIS変数対応） |
| UT-VAL-070〜079 | ポストインストール実行ファイルバリデーション（相対パス、拡張子チェック、存在確認） |

### ConfigurationService Tests (9テスト)

設定の保存・読み込み機能をテストします。

| テストID | テスト内容 |
|----------|-----------|
| UT-CFG-001 | 設定のXML保存 |
| UT-CFG-002 | 設定のXML読み込み |
| UT-CFG-003 | 存在しないファイルの読み込みエラー |
| UT-CFG-004 | 不正なXMLファイルの読み込みエラー |
| UT-CFG-005 | デフォルト設定の作成 |
| UT-CFG-006 | 日本語を含む設定のUTF-8保存 |
| UT-CFG-007 | RegistrationMode保存・読み込み |
| UT-CFG-008 | PostInstall設定の保存 |
| UT-CFG-009 | PostInstall設定の読み込み |

### NsisService Tests (32テスト)

NSISスクリプト生成機能をテストします。

| テストID | テスト内容 |
|----------|-----------|
| UT-NSIS-001 | 基本スクリプト生成 |
| UT-NSIS-002 | 日本語設定でのスクリプト生成 |
| UT-NSIS-003 | 日本語パスの処理 |
| UT-NSIS-004 | ファイルショートカットのCreateShortCut生成 |
| UT-NSIS-005 | フォルダーショートカットのshell32.dllアイコン設定 |
| UT-NSIS-006 | レジストリ無効時のコード除外・アンインストーラー配置 |
| UT-NSIS-007 | Program Files選択時の管理者権限設定 |
| UT-NSIS-008 | AppData選択時のユーザー権限設定 |
| UT-NSIS-009 | ビルドキャンセル時の例外処理 |
| UT-NSIS-010 | インストール後フォルダーを開く設定 |
| UT-NSIS-011 | インストール後ファイル実行設定 |
| UT-NSIS-012 | ユーザーデータ保持アンインストールスクリプト生成 |
| UT-NSIS-013 | 全削除アンインストールスクリプト生成 |
| UT-NSIS-014 | installed_files.dat生成（FILE:プレフィックス） |
| UT-NSIS-015 | installed_files.dat生成（DIR:プレフィックス） |
| UT-NSIS-016 | SetRegView64マクロ生成 |
| UT-NSIS-017 | インストールセクションでSetRegView64呼び出し |
| UT-NSIS-018 | アンインストールセクションでSetRegView64呼び出し |
| UT-NSIS-019 | 予約ファイル除外（File /x オプション） |
| UT-NSIS-020 | アップグレード時クリーンアップ処理生成 |
| UT-NSIS-021 | 会社名ありスタートメニューフォルダー生成 |
| UT-NSIS-022 | 会社名未入力時スタートメニューフォルダー生成（アプリ名使用） |
| UT-NSIS-023 | アンインストール時スタートメニュー削除（会社名あり） |
| UT-NSIS-024 | アンインストール時スタートメニュー削除（会社名未入力） |
| UT-NSIS-025 | 会社名あり時インストールパス生成 |
| UT-NSIS-026 | 会社名未入力時インストールパス生成（\\が連続しない） |
| UT-NSIS-027 | 会社名あり時レジストリパス生成 |
| UT-NSIS-028 | 会社名未入力時レジストリパス生成（\\が連続しない） |

### Converter Tests (14テスト)

WPFの値コンバーターをテストします。

| テストID | テスト内容 |
|----------|-----------|
| UT-CNV-001〜004 | EnumBoolConverter（enum↔bool変換） |
| UT-CNV-010〜013 | InverseBoolConverter（bool反転） |
| UT-CNV-020〜021 | InverseBoolToVisibilityConverter（bool→Visibility変換） |

## テスト結果

最新のテスト実行結果は [UnitTests.csv](UnitTests.csv) を参照してください。

### サマリー

| カテゴリ | テスト数 | 成功 | 失敗 |
|----------|---------|------|------|
| ValidationService | 64 | - | - |
| ConfigurationService | 9 | - | - |
| NsisService | 32 | - | - |
| Converters | 14 | - | - |
| **合計** | **119** | **-** | **-** |

※ テスト結果は実行時に更新されます

## テストプロジェクトの技術的な注意事項

### WPFプロジェクト参照の問題回避

WPFプロジェクトをテストプロジェクトから参照すると、ビルド時に生成される一時プロジェクト（`*_wpftmp`）がPackageReferenceを継承できない問題が発生します。

この問題を回避するため、テストプロジェクトでは以下のアプローチを採用しています：

1. **プロジェクト参照を使用しない** - WPFメインプロジェクトへの`ProjectReference`は使用しません
2. **ファイルリンク方式** - 必要なソースファイルを`<Compile Include="...">` でリンク
3. **FrameworkReference** - WPF型のために`<FrameworkReference Include="Microsoft.WindowsDesktop.App.WPF" />`を追加
4. **リソースのLogicalName** - 埋め込みリソースには`LogicalName`属性で正しい名前を指定

```xml
<!-- ファイルリンクの例 -->
<ItemGroup>
  <Compile Include="..\..\Services\ValidationService.cs" Link="Services\ValidationService.cs" />
</ItemGroup>

<!-- リソースリンクの例 -->
<ItemGroup>
  <EmbeddedResource Include="..\..\Resources\Strings.resx"
                    Link="Resources\Strings.resx"
                    LogicalName="EasyNSIS.Resources.Strings.resources" />
</ItemGroup>
```

### NsisServiceテストの制限

`BuildInstallerAsync`メソッドのテストは、`makensis.exe`の存在に依存します。テスト環境にNSISがインストールされていない場合、`Win32Exception`がスローされます。

このため、UT-NSIS-009（ビルドキャンセルテスト）では、`OperationCanceledException`と`Win32Exception`の両方を許容しています。

## 新しいテストの追加方法

1. 適切なテストファイルにテストメソッドを追加
2. テストID命名規則に従う：`UT_<カテゴリ>_<番号>_<メソッド名>_<条件>_<期待結果>`
3. [UnitTests.csv](UnitTests.csv) にテスト情報を追記
4. `dotnet test` で全テストが成功することを確認

### テストID命名規則

```
UT-VAL-001  → ValidationService テスト #001
UT-CFG-001  → ConfigurationService テスト #001
UT-NSIS-001 → NsisService テスト #001
UT-CNV-001  → Converter テスト #001
```

## 関連ドキュメント

- [テスト仕様書](TestSpec.md)
- [テスト結果一覧](UnitTests.csv)
