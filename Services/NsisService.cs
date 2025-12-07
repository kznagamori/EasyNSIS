using System.Diagnostics;
using System.IO;
using System.Text;
using EasyNSIS.Models;
using UtfUnknown;

namespace EasyNSIS.Services;

public class NsisService : INsisService
{
    private const string ExpectedNsisVersion = "v3.11";
    private readonly string _nsisPath;
    private readonly string _assetsPath;

    public NsisService()
    {
        var basePath = AppContext.BaseDirectory;
        _nsisPath = Path.Combine(basePath, "tools", "nsis-3.11", "makensis.exe");
        _assetsPath = Path.Combine(basePath, "assets");
    }

    public bool ValidateNsisInstallation(out string errorMessage)
    {
        errorMessage = string.Empty;

        if (!File.Exists(_nsisPath))
        {
            errorMessage = string.Format(Resources.Strings.Err_NsisNotFound, _nsisPath);
            return false;
        }

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _nsisPath,
                    Arguments = "/VERSION",
                    RedirectStandardOutput = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            var version = process.StandardOutput.ReadToEnd().Trim();
            process.WaitForExit();

            if (version != ExpectedNsisVersion)
            {
                errorMessage = string.Format(Resources.Strings.Err_NsisVersionMismatch, ExpectedNsisVersion, version);
                return false;
            }

            return true;
        }
        catch (Exception ex)
        {
            errorMessage = string.Format(Resources.Strings.Err_NsisValidationFailed, ex.Message);
            return false;
        }
    }

    public string GenerateScript(InstallerConfig config)
    {
        var sb = new StringBuilder();
        var now = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");

        // ヘッダー
        sb.AppendLine("; EasyNSIS Generated Script");
        sb.AppendLine($"; Generated: {now}");
        sb.AppendLine();

        // インクルード
        sb.AppendLine("!include \"MUI2.nsh\"");
        sb.AppendLine("!include \"FileFunc.nsh\"");
        sb.AppendLine("!include \"WordFunc.nsh\"");
        sb.AppendLine("!include \"x64.nsh\"");
        sb.AppendLine();

        // 基本設定
        var appName = config.BasicInfo.ApplicationName;
        var companyName = config.BasicInfo.CompanyName;
        var version = config.BasicInfo.Version;
        var outputFileName = string.IsNullOrEmpty(version)
            ? $"{appName}_Setup.exe"
            : $"{appName}_{version}_Setup.exe";

        sb.AppendLine($"Name \"{appName}\"");
        sb.AppendLine($"OutFile \"{outputFileName}\"");
        sb.AppendLine($"InstallDir \"{GetInstallDir(config)}\"");

        // レジストリを使用する場合のみInstallDirRegKey
        if (config.BasicInfo.RegistrationMode != RegistrationMode.NoRegistry)
        {
            var regRoot = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "HKLM" : "HKCU";
            var regSubPath = string.IsNullOrWhiteSpace(companyName) ? appName : $"{companyName}\\{appName}";
            sb.AppendLine($"InstallDirRegKey {regRoot} \"Software\\{regSubPath}\" \"InstallLocation\"");
        }

        // 権限レベル
        var execLevel = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "admin" : "user";
        sb.AppendLine($"RequestExecutionLevel {execLevel}");
        sb.AppendLine("SetCompressor /SOLID lzma");
        sb.AppendLine("Unicode True");
        sb.AppendLine();

        // 64ビットレジストリビュー（x64ターゲット）
        sb.AppendLine("; Use 64-bit registry view");
        sb.AppendLine("!macro SetRegView64");
        sb.AppendLine("    ${If} ${RunningX64}");
        sb.AppendLine("        SetRegView 64");
        sb.AppendLine("    ${EndIf}");
        sb.AppendLine("!macroend");
        sb.AppendLine();

        // バージョン情報
        var versionFull = NormalizeVersion(version);
        sb.AppendLine($"VIProductVersion \"{versionFull}\"");
        sb.AppendLine($"VIAddVersionKey \"ProductName\" \"{appName}\"");
        sb.AppendLine($"VIAddVersionKey \"CompanyName\" \"{companyName}\"");
        sb.AppendLine($"VIAddVersionKey \"ProductVersion\" \"{version}\"");
        sb.AppendLine($"VIAddVersionKey \"FileVersion\" \"{version}\"");
        sb.AppendLine($"VIAddVersionKey \"FileDescription\" \"{appName} Installer\"");
        sb.AppendLine($"VIAddVersionKey \"LegalCopyright\" \"© {companyName}\"");
        sb.AppendLine();

        // アイコン設定
        if (!string.IsNullOrWhiteSpace(config.BasicInfo.InstallerIcon) && File.Exists(config.BasicInfo.InstallerIcon))
        {
            sb.AppendLine($"!define MUI_ICON \"{config.BasicInfo.InstallerIcon}\"");
            sb.AppendLine($"!define MUI_UNICON \"{config.BasicInfo.InstallerIcon}\"");
            sb.AppendLine();
        }

        // ライセンスファイルの準備
        var licenseFile = GetLicenseFile(config);

        // MUIページ
        sb.AppendLine("!insertmacro MUI_PAGE_WELCOME");
        sb.AppendLine($"!insertmacro MUI_PAGE_LICENSE \"{licenseFile}\"");
        if (config.InstallDestination.AllowUserChange)
        {
            sb.AppendLine("!insertmacro MUI_PAGE_DIRECTORY");
        }
        sb.AppendLine("!insertmacro MUI_PAGE_INSTFILES");
        sb.AppendLine("!insertmacro MUI_PAGE_FINISH");
        sb.AppendLine();

        sb.AppendLine("!insertmacro MUI_UNPAGE_CONFIRM");
        sb.AppendLine("!insertmacro MUI_UNPAGE_INSTFILES");
        sb.AppendLine();

        // 言語設定
        if (config.BasicInfo.Language.Type == LanguageType.Locale)
        {
            sb.AppendLine("!insertmacro MUI_LANGUAGE \"Japanese\"");
            sb.AppendLine("!insertmacro MUI_LANGUAGE \"English\"");
        }
        else
        {
            var lang = config.BasicInfo.Language.Value == "ja-JP" ? "Japanese" : "English";
            sb.AppendLine($"!insertmacro MUI_LANGUAGE \"{lang}\"");
        }
        sb.AppendLine();

        // 変数定義
        sb.AppendLine("Var InstalledVersion");
        sb.AppendLine("Var NewVersion");
        sb.AppendLine();

        // バージョン比較関数
        GenerateVersionCompareFunction(sb);

        // インストールセクション
        sb.AppendLine("Section \"Install\"");
        sb.AppendLine("    ; Set 64-bit registry view");
        sb.AppendLine("    !insertmacro SetRegView64");
        sb.AppendLine();
        sb.AppendLine("    SetOutPath \"$INSTDIR\"");
        sb.AppendLine();

        // 既存インストールのチェック
        GenerateVersionCheck(sb, config);

        // ファイルコピー（予約ファイルを除外）
        sb.AppendLine($"    File /r /x \"installed_files.dat\" /x \"installed_version.dat\" /x \"Uninstall.exe\" \"{config.SourceFolder.Path}\\*.*\"");
        sb.AppendLine();

        // バージョンファイルの作成
        sb.AppendLine($"    FileOpen $0 \"$INSTDIR\\installed_version.dat\" w");
        sb.AppendLine($"    FileWrite $0 \"{version}\"");
        sb.AppendLine("    FileClose $0");
        sb.AppendLine();

        // installed_files.datの作成
        GenerateInstalledFilesList(sb, config);

        // アンインストーラー作成
        sb.AppendLine("    WriteUninstaller \"$INSTDIR\\Uninstall.exe\"");
        sb.AppendLine();

        // ショートカット作成
        GenerateShortcuts(sb, config);

        // レジストリ登録
        if (config.BasicInfo.RegistrationMode != RegistrationMode.NoRegistry)
        {
            GenerateRegistryEntries(sb, config);
        }

        // インストール後処理
        GeneratePostInstallActions(sb, config);

        sb.AppendLine("SectionEnd");
        sb.AppendLine();

        // アンインストールセクション
        GenerateUninstallSection(sb, config);

        return sb.ToString();
    }

    public async Task<(bool Success, string Output)> BuildInstallerAsync(
        string scriptPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default)
    {
        var output = new StringBuilder();

        // NSISはシステムのコードページで出力するため、システムのデフォルトエンコーディングを使用
        // 日本語WindowsではCP932 (Shift_JIS)
        var systemEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _nsisPath,
                Arguments = $"\"{scriptPath}\"",
                WorkingDirectory = Path.GetDirectoryName(scriptPath),
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
                StandardOutputEncoding = systemEncoding,
                StandardErrorEncoding = systemEncoding
            },
            EnableRaisingEvents = true
        };

        process.OutputDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };

        process.ErrorDataReceived += (_, e) =>
        {
            if (e.Data != null)
            {
                output.AppendLine(e.Data);
                progress?.Report(e.Data);
            }
        };

        try
        {
            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            await process.WaitForExitAsync(cancellationToken);

            return (process.ExitCode == 0, output.ToString());
        }
        catch (OperationCanceledException)
        {
            try
            {
                process.Kill(entireProcessTree: true);
            }
            catch { }
            throw;
        }
    }

    private string GetInstallDir(InstallerConfig config)
    {
        var company = config.BasicInfo.CompanyName;
        var app = config.BasicInfo.ApplicationName;

        // 会社名が空の場合はアプリ名のみ
        var subPath = string.IsNullOrWhiteSpace(company) ? app : $"{company}\\{app}";

        return config.InstallDestination.Type switch
        {
            InstallDestinationType.AppDataRoaming => $"$APPDATA\\{subPath}",
            InstallDestinationType.AppDataLocal => $"$LOCALAPPDATA\\{subPath}",
            InstallDestinationType.ProgramFiles => $"$PROGRAMFILES64\\{subPath}",
            InstallDestinationType.Custom => config.InstallDestination.CustomPath,
            _ => $"$APPDATA\\{subPath}"
        };
    }

    private string GetLicenseFile(InstallerConfig config)
    {
        // NSISのライセンス表示はシステムのコードページを使用するため、
        // 日本語WindowsではCP932（Shift_JIS）に変換する必要がある
        Encoding? systemEncoding = null;
        try
        {
            systemEncoding = Encoding.GetEncoding(System.Globalization.CultureInfo.CurrentCulture.TextInfo.ANSICodePage);
        }
        catch
        {
            // .NET Coreではコードページエンコーディングがデフォルトで利用できない場合がある
            // その場合は変換せずにUTF-8を使用
        }

        var licenseFileName = $"{config.BasicInfo.ApplicationName}_License.txt";
        var tempPath = Path.Combine(config.Output.Folder, licenseFileName);

        if (config.License.Type == LicenseType.File && File.Exists(config.License.FilePath))
        {
            // ReadJEncでファイルの文字コードを自動判別して読み込み
            var content = ReadFileWithAutoDetectEncoding(config.License.FilePath);
            File.WriteAllText(tempPath, content, systemEncoding ?? Encoding.UTF8);
            return tempPath;
        }

        if (config.License.Type == LicenseType.Text && !string.IsNullOrWhiteSpace(config.License.Text))
        {
            // テキストをシステムエンコーディングで一時ファイルに保存
            File.WriteAllText(tempPath, config.License.Text, systemEncoding ?? Encoding.UTF8);
            return tempPath;
        }

        // デフォルトライセンス - ReadJEncで文字コードを自動判別して読み込み
        var lang = config.BasicInfo.Language.Value == "ja-JP" ? "ja" : "en";
        var defaultLicense = Path.Combine(_assetsPath, $"license-{lang}.txt");
        if (File.Exists(defaultLicense))
        {
            var content = ReadFileWithAutoDetectEncoding(defaultLicense);
            File.WriteAllText(tempPath, content, systemEncoding ?? Encoding.UTF8);
            return tempPath;
        }

        return string.Empty;
    }

    /// <summary>
    /// UTF-unknownを使用してファイルの文字コードを自動判別し、テキストを読み込む
    /// </summary>
    private static string ReadFileWithAutoDetectEncoding(string filePath)
    {
        try
        {
            // UTF-unknownで文字コードを自動判別
            var result = CharsetDetector.DetectFromFile(filePath);
            var detected = result.Detected;

            if (detected?.Encoding != null)
            {
                return File.ReadAllText(filePath, detected.Encoding);
            }

            // 判別できなかった場合はUTF-8として読み込む
            return File.ReadAllText(filePath, Encoding.UTF8);
        }
        catch
        {
            // 例外が発生した場合はUTF-8として読み込む
            return File.ReadAllText(filePath, Encoding.UTF8);
        }
    }

    private static string NormalizeVersion(string version)
    {
        if (string.IsNullOrEmpty(version))
            return "0.0.0.0";

        var parts = version.Split('.');
        var result = new string[4];
        for (int i = 0; i < 4; i++)
        {
            result[i] = i < parts.Length ? parts[i] : "0";
        }
        return string.Join(".", result);
    }

    private static void GenerateVersionCompareFunction(StringBuilder sb)
    {
        sb.AppendLine("Function CompareVersions");
        sb.AppendLine("    ; Compare $InstalledVersion with $NewVersion");
        sb.AppendLine("    ; Returns: $0 = 0 if equal, 1 if new > installed, -1 if new < installed");
        sb.AppendLine("    Push $1");
        sb.AppendLine("    Push $2");
        sb.AppendLine("    Push $3");
        sb.AppendLine("    Push $4");
        sb.AppendLine();
        sb.AppendLine("    StrCpy $0 0");
        sb.AppendLine();
        sb.AppendLine("    ; Simple string comparison for now");
        sb.AppendLine("    StrCmp $InstalledVersion $NewVersion equal");
        sb.AppendLine("    StrCmp $InstalledVersion \"\" new_is_newer");
        sb.AppendLine("    StrCmp $NewVersion \"\" installed_is_newer");
        sb.AppendLine();
        sb.AppendLine("    ; Use VersionCompare from WordFunc");
        sb.AppendLine("    ${VersionCompare} $NewVersion $InstalledVersion $0");
        sb.AppendLine("    Goto done");
        sb.AppendLine();
        sb.AppendLine("equal:");
        sb.AppendLine("    StrCpy $0 0");
        sb.AppendLine("    Goto done");
        sb.AppendLine();
        sb.AppendLine("new_is_newer:");
        sb.AppendLine("    StrCpy $0 1");
        sb.AppendLine("    Goto done");
        sb.AppendLine();
        sb.AppendLine("installed_is_newer:");
        sb.AppendLine("    StrCpy $0 2");
        sb.AppendLine("    Goto done");
        sb.AppendLine();
        sb.AppendLine("done:");
        sb.AppendLine("    Pop $4");
        sb.AppendLine("    Pop $3");
        sb.AppendLine("    Pop $2");
        sb.AppendLine("    Pop $1");
        sb.AppendLine("FunctionEnd");
        sb.AppendLine();
    }

    private static void GenerateVersionCheck(StringBuilder sb, InstallerConfig config)
    {
        var version = config.BasicInfo.Version;

        sb.AppendLine("    ; Check for existing installation");
        sb.AppendLine("    IfFileExists \"$INSTDIR\\*.*\" 0 new_install");
        sb.AppendLine();
        sb.AppendLine("    ; Read installed version");
        sb.AppendLine("    StrCpy $InstalledVersion \"\"");
        sb.AppendLine("    IfFileExists \"$INSTDIR\\installed_version.dat\" 0 check_registry");
        sb.AppendLine("    FileOpen $0 \"$INSTDIR\\installed_version.dat\" r");
        sb.AppendLine("    FileRead $0 $InstalledVersion");
        sb.AppendLine("    FileClose $0");
        sb.AppendLine("    Goto compare_versions");
        sb.AppendLine();

        if (config.BasicInfo.RegistrationMode != RegistrationMode.NoRegistry)
        {
            var regRoot = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "HKLM" : "HKCU";
            sb.AppendLine("check_registry:");
            sb.AppendLine($"    ReadRegStr $InstalledVersion {regRoot} \"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{config.BasicInfo.ApplicationName}\" \"DisplayVersion\"");
            sb.AppendLine("    StrCmp $InstalledVersion \"\" version_not_found compare_versions");
        }
        else
        {
            sb.AppendLine("check_registry:");
            sb.AppendLine("    Goto version_not_found");
        }

        sb.AppendLine();
        sb.AppendLine("version_not_found:");
        sb.AppendLine("    MessageBox MB_OK|MB_ICONSTOP \"インストール済みバージョンを判定できません。続行できません。$\\n$\\nインストール先フォルダーを開きます。手動で削除してください。\"");

        if (config.BasicInfo.RegistrationMode != RegistrationMode.NoRegistry)
        {
            var regRoot = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "HKLM" : "HKCU";
            sb.AppendLine($"    DeleteRegKey {regRoot} \"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{config.BasicInfo.ApplicationName}\"");
        }

        sb.AppendLine("    ExecShell \"open\" \"$INSTDIR\"");
        sb.AppendLine("    Abort");
        sb.AppendLine();

        sb.AppendLine("compare_versions:");
        sb.AppendLine($"    StrCpy $NewVersion \"{version}\"");
        sb.AppendLine("    Call CompareVersions");
        sb.AppendLine("    IntCmp $0 1 upgrade_install same_or_older same_or_older");
        sb.AppendLine();

        sb.AppendLine("same_or_older:");
        sb.AppendLine("    MessageBox MB_OK|MB_ICONSTOP \"同じか、より古いバージョンがインストール済みです。\"");
        sb.AppendLine("    Abort");
        sb.AppendLine();

        sb.AppendLine("upgrade_install:");
        sb.AppendLine("    ; Perform user data preserving cleanup before upgrade");
        sb.AppendLine("    IfFileExists \"$INSTDIR\\installed_files.dat\" 0 skip_cleanup");
        sb.AppendLine();
        sb.AppendLine("    ; Read and delete files from installed_files.dat");
        sb.AppendLine("    FileOpen $0 \"$INSTDIR\\installed_files.dat\" r");
        sb.AppendLine("    IfErrors skip_cleanup");
        sb.AppendLine();
        sb.AppendLine("upgrade_read_loop:");
        sb.AppendLine("    FileRead $0 $1");
        sb.AppendLine("    IfErrors upgrade_done_reading");
        sb.AppendLine();
        sb.AppendLine("    ; Trim trailing newline/carriage return");
        sb.AppendLine("    StrCpy $2 $1 -2");
        sb.AppendLine();
        sb.AppendLine("    ; Skip if empty");
        sb.AppendLine("    StrCmp $2 \"\" upgrade_read_loop");
        sb.AppendLine();
        sb.AppendLine("    ; Check for .. (path traversal) - skip");
        sb.AppendLine("    ${WordFind} $2 \"..\" \"E+1{\" $3");
        sb.AppendLine("    IfErrors upgrade_no_traversal");
        sb.AppendLine("    Goto upgrade_read_loop");
        sb.AppendLine();
        sb.AppendLine("upgrade_no_traversal:");
        sb.AppendLine("    ; Check if it's a file (FILE: prefix) or directory (DIR: prefix)");
        sb.AppendLine("    StrCpy $3 $2 5");
        sb.AppendLine("    StrCmp $3 \"FILE:\" upgrade_is_file");
        sb.AppendLine("    StrCpy $3 $2 4");
        sb.AppendLine("    StrCmp $3 \"DIR:\" upgrade_is_dir");
        sb.AppendLine("    ; Unknown prefix, treat as file");
        sb.AppendLine("    StrCpy $4 $2");
        sb.AppendLine("    Goto upgrade_delete_file");
        sb.AppendLine();
        sb.AppendLine("upgrade_is_file:");
        sb.AppendLine("    StrCpy $4 $2 \"\" 5");
        sb.AppendLine("upgrade_delete_file:");
        sb.AppendLine("    Delete \"$INSTDIR\\$4\"");
        sb.AppendLine("    Goto upgrade_read_loop");
        sb.AppendLine();
        sb.AppendLine("upgrade_is_dir:");
        sb.AppendLine("    StrCpy $4 $2 \"\" 4");
        sb.AppendLine("    RMDir \"$INSTDIR\\$4\"");
        sb.AppendLine("    Goto upgrade_read_loop");
        sb.AppendLine();
        sb.AppendLine("upgrade_done_reading:");
        sb.AppendLine("    FileClose $0");
        sb.AppendLine();
        sb.AppendLine("skip_cleanup:");
        sb.AppendLine();

        sb.AppendLine("new_install:");
        sb.AppendLine();
    }

    private static void GenerateInstalledFilesList(StringBuilder sb, InstallerConfig config)
    {
        sb.AppendLine("    ; Create installed files list");
        sb.AppendLine("    FileOpen $0 \"$INSTDIR\\installed_files.dat\" w");

        var files = new List<string>();
        var directories = new List<string>();

        // インストール対象フォルダー内のファイルとディレクトリ一覧を再帰的に取得
        if (Directory.Exists(config.SourceFolder.Path))
        {
            var baseDir = config.SourceFolder.Path;

            // ファイル一覧
            foreach (var file in Directory.EnumerateFiles(baseDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(baseDir, file).Replace('/', '\\');

                // 予約ファイル名をスキップ
                if (relativePath.Equals("installed_files.dat", StringComparison.OrdinalIgnoreCase) ||
                    relativePath.Equals("installed_version.dat", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                files.Add(relativePath);
            }

            // ディレクトリ一覧（深い階層から順にするため、パスの長さでソート）
            foreach (var dir in Directory.EnumerateDirectories(baseDir, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(baseDir, dir).Replace('/', '\\');
                directories.Add(relativePath);
            }
            // 深い階層から削除するため、パスの長さで降順ソート
            directories = directories.OrderByDescending(d => d.Length).ToList();
        }

        // ファイルを書き込み（FILE: プレフィックス）
        foreach (var file in files)
        {
            sb.AppendLine($"    FileWrite $0 \"FILE:{file}$\\r$\\n\"");
        }

        // システムが生成するファイル
        sb.AppendLine("    FileWrite $0 \"FILE:Uninstall.exe$\\r$\\n\"");
        sb.AppendLine("    FileWrite $0 \"FILE:installed_version.dat$\\r$\\n\"");
        sb.AppendLine("    FileWrite $0 \"FILE:installed_files.dat$\\r$\\n\"");

        // ディレクトリを書き込み（DIR: プレフィックス、深い階層から順に）
        foreach (var dir in directories)
        {
            sb.AppendLine($"    FileWrite $0 \"DIR:{dir}$\\r$\\n\"");
        }

        sb.AppendLine("    FileClose $0");
        sb.AppendLine();
    }

    private static void GenerateShortcuts(StringBuilder sb, InstallerConfig config)
    {
        // 会社名が未入力の場合はアプリケーション名を使用
        var startMenuFolder = !string.IsNullOrWhiteSpace(config.BasicInfo.CompanyName)
            ? config.BasicInfo.CompanyName
            : config.BasicInfo.ApplicationName;

        // デスクトップショートカット
        if (config.Shortcuts.Desktop.Count > 0)
        {
            sb.AppendLine("    ; Desktop shortcuts");
            foreach (var item in config.Shortcuts.Desktop)
            {
                var name = Path.GetFileNameWithoutExtension(item.Path);
                if (item.Type == ShortcutItemType.File)
                {
                    // ファイルの場合：ターゲットファイル自体のアイコンを使用
                    sb.AppendLine($"    CreateShortCut \"$DESKTOP\\{name}.lnk\" \"$INSTDIR\\{item.Path}\"");
                }
                else
                {
                    // フォルダーの場合：shell32.dllのフォルダーアイコン（インデックス3）を使用
                    sb.AppendLine($"    CreateShortCut \"$DESKTOP\\{name}.lnk\" \"$INSTDIR\\{item.Path}\" \"\" \"$SYSDIR\\shell32.dll\" 3");
                }
            }
            sb.AppendLine();
        }

        // スタートメニューショートカット
        sb.AppendLine("    ; Start menu shortcuts");
        sb.AppendLine($"    CreateDirectory \"$SMPROGRAMS\\{startMenuFolder}\"");

        foreach (var item in config.Shortcuts.StartMenu)
        {
            var name = Path.GetFileNameWithoutExtension(item.Path);
            if (item.Type == ShortcutItemType.File)
            {
                // ファイルの場合：ターゲットファイル自体のアイコンを使用
                sb.AppendLine($"    CreateShortCut \"$SMPROGRAMS\\{startMenuFolder}\\{name}.lnk\" \"$INSTDIR\\{item.Path}\"");
            }
            else
            {
                // フォルダーの場合：shell32.dllのフォルダーアイコン（インデックス3）を使用
                sb.AppendLine($"    CreateShortCut \"$SMPROGRAMS\\{startMenuFolder}\\{name}.lnk\" \"$INSTDIR\\{item.Path}\" \"\" \"$SYSDIR\\shell32.dll\" 3");
            }
        }

        // アンインストールショートカット
        sb.AppendLine($"    CreateShortCut \"$SMPROGRAMS\\{startMenuFolder}\\Uninstall.lnk\" \"$INSTDIR\\Uninstall.exe\"");
        sb.AppendLine();
    }

    private static void GenerateRegistryEntries(StringBuilder sb, InstallerConfig config)
    {
        var company = config.BasicInfo.CompanyName;
        var app = config.BasicInfo.ApplicationName;
        var version = config.BasicInfo.Version;
        var regRoot = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "HKLM" : "HKCU";

        // 会社名が空の場合はアプリ名のみ
        var regSubPath = string.IsNullOrWhiteSpace(company) ? app : $"{company}\\{app}";

        sb.AppendLine("    ; Registry entries");
        sb.AppendLine($"    WriteRegStr {regRoot} \"Software\\{regSubPath}\" \"InstallLocation\" \"$INSTDIR\"");
        sb.AppendLine($"    WriteRegStr {regRoot} \"Software\\{regSubPath}\" \"Version\" \"{version}\"");
        sb.AppendLine();

        // アプリと機能に登録する場合のみアンインストール情報を書き込む
        if (config.BasicInfo.RegistrationMode == RegistrationMode.RegisterToAppsAndFeatures)
        {
            sb.AppendLine("    ; Add/Remove Programs entry");
            var uninstKey = $"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{app}";
            sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"DisplayName\" \"{app}\"");
            sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"DisplayVersion\" \"{version}\"");
            sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"Publisher\" \"{company}\"");
            sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"InstallLocation\" \"$INSTDIR\"");
            sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"UninstallString\" \"$INSTDIR\\Uninstall.exe\"");

            if (!string.IsNullOrWhiteSpace(config.BasicInfo.InstallerIcon))
            {
                sb.AppendLine($"    WriteRegStr {regRoot} \"{uninstKey}\" \"DisplayIcon\" \"{config.BasicInfo.InstallerIcon}\"");
            }

            sb.AppendLine($"    WriteRegDWORD {regRoot} \"{uninstKey}\" \"NoModify\" 1");
            sb.AppendLine($"    WriteRegDWORD {regRoot} \"{uninstKey}\" \"NoRepair\" 1");
            sb.AppendLine();
        }
    }

    private static void GeneratePostInstallActions(StringBuilder sb, InstallerConfig config)
    {
        var hasPostInstallActions = config.PostInstall.OpenInstallFolder || config.PostInstall.RunAfterInstall;
        if (!hasPostInstallActions)
            return;

        sb.AppendLine("    ; Post-install actions");

        if (config.PostInstall.OpenInstallFolder)
        {
            sb.AppendLine("    ExecShell \"open\" \"$INSTDIR\"");
        }

        if (config.PostInstall.RunAfterInstall && !string.IsNullOrWhiteSpace(config.PostInstall.RunAfterInstallPath))
        {
            var filePath = config.PostInstall.RunAfterInstallPath.Replace('/', '\\');
            sb.AppendLine($"    ExecShell \"open\" \"$INSTDIR\\{filePath}\"");
        }

        sb.AppendLine();
    }

    private static void GenerateUninstallSection(StringBuilder sb, InstallerConfig config)
    {
        var company = config.BasicInfo.CompanyName;
        var app = config.BasicInfo.ApplicationName;
        // 会社名が未入力の場合はアプリケーション名を使用（スタートメニューフォルダー用）
        var startMenuFolder = !string.IsNullOrWhiteSpace(company) ? company : app;
        var regRoot = config.InstallDestination.Type == InstallDestinationType.ProgramFiles ? "HKLM" : "HKCU";
        // 会社名が空の場合はアプリ名のみ
        var regSubPath = string.IsNullOrWhiteSpace(company) ? app : $"{company}\\{app}";

        sb.AppendLine("Section \"Uninstall\"");
        sb.AppendLine("    ; Set 64-bit registry view");
        sb.AppendLine("    !insertmacro SetRegView64");
        sb.AppendLine();

        // $INSTDIR の存在確認
        sb.AppendLine("    ; Check if $INSTDIR exists");
        sb.AppendLine("    IfFileExists \"$INSTDIR\\*.*\" instdir_exists 0");
        sb.AppendLine("    MessageBox MB_OK|MB_ICONEXCLAMATION \"インストール先フォルダーが見つかりません: $INSTDIR\"");
        sb.AppendLine("    Goto cleanup_shortcuts");
        sb.AppendLine();
        sb.AppendLine("instdir_exists:");

        if (config.Uninstall.Cleanup == CleanupType.All)
        {
            // すべて削除モード
            sb.AppendLine("    ; Remove all files in $INSTDIR");
            sb.AppendLine("    ClearErrors");
            sb.AppendLine("    RMDir /r \"$INSTDIR\"");
            sb.AppendLine("    IfErrors 0 cleanup_shortcuts");
            sb.AppendLine("    MessageBox MB_OK|MB_ICONEXCLAMATION \"一部のファイルを削除できませんでした。ファイルがロックされている可能性があります。\"");
            sb.AppendLine("    Goto cleanup_shortcuts");
        }
        else
        {
            // ユーザーデータ保護モード: installed_files.dat を読み込んでリストされたファイルのみ削除
            sb.AppendLine("    ; Remove only installed files (preserve user data)");
            sb.AppendLine("    IfFileExists \"$INSTDIR\\installed_files.dat\" 0 no_file_list");
            sb.AppendLine();
            sb.AppendLine("    ; Read and delete files/directories from installed_files.dat");
            sb.AppendLine("    FileOpen $0 \"$INSTDIR\\installed_files.dat\" r");
            sb.AppendLine("    IfErrors no_file_list");
            sb.AppendLine();
            sb.AppendLine("read_loop:");
            sb.AppendLine("    FileRead $0 $1");
            sb.AppendLine("    IfErrors done_reading");
            sb.AppendLine();
            sb.AppendLine("    ; Trim trailing newline/carriage return");
            sb.AppendLine("    StrCpy $2 $1 -2");
            sb.AppendLine();
            sb.AppendLine("    ; Skip if empty");
            sb.AppendLine("    StrCmp $2 \"\" read_loop");
            sb.AppendLine();
            sb.AppendLine("    ; Check for .. (path traversal) - skip with warning");
            sb.AppendLine("    ${WordFind} $2 \"..\" \"E+1{\" $3");
            sb.AppendLine("    IfErrors no_traversal");
            sb.AppendLine("    ; Contains .., skip this entry");
            sb.AppendLine("    DetailPrint \"警告: 不正なパスをスキップしました: $2\"");
            sb.AppendLine("    Goto read_loop");
            sb.AppendLine();
            sb.AppendLine("no_traversal:");
            sb.AppendLine("    ; Check if it's a file (FILE: prefix) or directory (DIR: prefix)");
            sb.AppendLine("    StrCpy $3 $2 5");
            sb.AppendLine("    StrCmp $3 \"FILE:\" is_file");
            sb.AppendLine("    StrCpy $3 $2 4");
            sb.AppendLine("    StrCmp $3 \"DIR:\" is_dir");
            sb.AppendLine("    ; Unknown prefix, treat as file for backward compatibility");
            sb.AppendLine("    Goto delete_as_file");
            sb.AppendLine();
            sb.AppendLine("is_file:");
            sb.AppendLine("    ; Extract path after FILE: prefix");
            sb.AppendLine("    StrCpy $4 $2 \"\" 5");
            sb.AppendLine("    Goto delete_as_file_path");
            sb.AppendLine();
            sb.AppendLine("delete_as_file:");
            sb.AppendLine("    StrCpy $4 $2");
            sb.AppendLine("delete_as_file_path:");
            sb.AppendLine("    ClearErrors");
            sb.AppendLine("    Delete \"$INSTDIR\\$4\"");
            sb.AppendLine("    IfErrors 0 read_loop");
            sb.AppendLine("    ; File locked or access denied");
            sb.AppendLine("    DetailPrint \"警告: ファイルを削除できませんでした (ロック中の可能性): $INSTDIR\\$4\"");
            sb.AppendLine("    Goto read_loop");
            sb.AppendLine();
            sb.AppendLine("is_dir:");
            sb.AppendLine("    ; Extract path after DIR: prefix");
            sb.AppendLine("    StrCpy $4 $2 \"\" 4");
            sb.AppendLine("    ClearErrors");
            sb.AppendLine("    RMDir \"$INSTDIR\\$4\"");
            sb.AppendLine("    ; Don't warn if directory not empty (user data preserved)");
            sb.AppendLine("    Goto read_loop");
            sb.AppendLine();
            sb.AppendLine("done_reading:");
            sb.AppendLine("    FileClose $0");
            sb.AppendLine();
            sb.AppendLine("    ; Try to remove $INSTDIR if empty");
            sb.AppendLine("    RMDir \"$INSTDIR\"");
            sb.AppendLine("    Goto cleanup_shortcuts");
            sb.AppendLine();
            sb.AppendLine("no_file_list:");
            sb.AppendLine("    MessageBox MB_OK|MB_ICONEXCLAMATION \"installed_files.dat が見つかりません。ファイルの削除をスキップします。\"");
        }

        sb.AppendLine();
        sb.AppendLine("cleanup_shortcuts:");

        // ショートカット削除
        sb.AppendLine("    ; Remove shortcuts");
        foreach (var item in config.Shortcuts.Desktop)
        {
            var name = Path.GetFileNameWithoutExtension(item.Path);
            sb.AppendLine($"    Delete \"$DESKTOP\\{name}.lnk\"");
        }

        sb.AppendLine($"    RMDir /r \"$SMPROGRAMS\\{startMenuFolder}\"");
        sb.AppendLine();

        // レジストリ削除
        if (config.BasicInfo.RegistrationMode != RegistrationMode.NoRegistry)
        {
            sb.AppendLine("    ; Remove registry entries");
            sb.AppendLine($"    DeleteRegKey {regRoot} \"Software\\{regSubPath}\"");
            sb.AppendLine($"    DeleteRegKey {regRoot} \"Software\\Microsoft\\Windows\\CurrentVersion\\Uninstall\\{app}\"");
            sb.AppendLine();
        }

        // 自己削除
        sb.AppendLine("    ; Self-delete uninstaller");
        sb.AppendLine("    Delete /REBOOTOK \"$INSTDIR\\Uninstall.exe\"");
        sb.AppendLine("    RMDir \"$INSTDIR\"");

        sb.AppendLine("SectionEnd");
    }
}
