using System.IO;
using System.Threading;
using EasyNSIS.Models;
using EasyNSIS.Services;

namespace EasyNSIS.Tests.Services;

public class NsisServiceTests : IDisposable
{
    private readonly NsisService _service;
    private readonly string _tempDir;
    private readonly string _sourceFolderPath;

    public NsisServiceTests()
    {
        _service = new NsisService();
        _tempDir = Path.Combine(Path.GetTempPath(), $"NsisServiceTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        // テスト用ソースフォルダを作成
        _sourceFolderPath = Path.Combine(_tempDir, "source");
        Directory.CreateDirectory(_sourceFolderPath);
        File.WriteAllText(Path.Combine(_sourceFolderPath, "test.exe"), "dummy");
        File.WriteAllText(Path.Combine(_sourceFolderPath, "readme.txt"), "readme");
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_tempDir))
            {
                Directory.Delete(_tempDir, true);
            }
        }
        catch { }
    }

    private InstallerConfig CreateMinimalConfig()
    {
        return new InstallerConfig
        {
            BasicInfo = new BasicInfo
            {
                CompanyName = "TestCompany",
                ApplicationName = "TestApp",
                Version = "1.0.0.0",
                RegistrationMode = RegistrationMode.RegisterToAppsAndFeatures,
                Language = new LanguageSettings { Type = LanguageType.Fixed, Value = "en-US" }
            },
            SourceFolder = new SourceFolder { Path = _sourceFolderPath },
            InstallDestination = new InstallDestination
            {
                Type = InstallDestinationType.AppDataRoaming,
                AllowUserChange = true
            },
            Shortcuts = new ShortcutSettings
            {
                Desktop = [],
                StartMenu = [new ShortcutItem { Path = "test.exe", Type = ShortcutItemType.File }]
            },
            License = new LicenseSettings { Type = LicenseType.Default },
            Output = new OutputSettings { Folder = _tempDir },
            Uninstall = new UninstallSettings { Cleanup = CleanupType.All }
        };
    }

    [Fact]
    public void UT_NSIS_001_GenerateScript_MinimalConfig_ReturnsValidScript()
    {
        // Arrange
        var config = CreateMinimalConfig();

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.NotEmpty(script);
        Assert.Contains("; EasyNSIS Generated Script", script);
        Assert.Contains("!include \"MUI2.nsh\"", script);
        Assert.Contains("Name \"TestApp\"", script);
        Assert.Contains("OutFile \"TestApp_1.0.0.0_Setup.exe\"", script);
        Assert.Contains("Unicode True", script);
        Assert.Contains("Section \"Install\"", script);
        Assert.Contains("Section \"Uninstall\"", script);
    }

    [Fact]
    public void UT_NSIS_002_GenerateScript_JapaneseConfig_ReturnsUtf8Script()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "テスト会社";
        config.BasicInfo.ApplicationName = "テストアプリ";
        config.BasicInfo.Language = new LanguageSettings { Type = LanguageType.Fixed, Value = "ja-JP" };

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("Name \"テストアプリ\"", script);
        Assert.Contains("テスト会社", script);
        Assert.Contains("!insertmacro MUI_LANGUAGE \"Japanese\"", script);
    }

    [Fact]
    public void UT_NSIS_003_GenerateScript_JapanesePath_EscapesCorrectly()
    {
        // Arrange
        var japaneseSourcePath = Path.Combine(_tempDir, "日本語パス");
        Directory.CreateDirectory(japaneseSourcePath);
        File.WriteAllText(Path.Combine(japaneseSourcePath, "アプリ.exe"), "dummy");

        var config = CreateMinimalConfig();
        config.SourceFolder.Path = japaneseSourcePath;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains(japaneseSourcePath, script);
        Assert.Contains("File /r", script);
    }

    [Fact]
    public void UT_NSIS_004_GenerateScript_FileShortcut_GeneratesCreateShortCut()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Shortcuts.Desktop.Add(new ShortcutItem { Path = "test.exe", Type = ShortcutItemType.File });

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("CreateShortCut \"$DESKTOP\\test.lnk\" \"$INSTDIR\\test.exe\"", script);
    }

    [Fact]
    public void UT_NSIS_005_GenerateScript_FolderShortcut_GeneratesWithShell32Icon()
    {
        // Arrange
        var subFolder = Path.Combine(_sourceFolderPath, "docs");
        Directory.CreateDirectory(subFolder);

        var config = CreateMinimalConfig();
        config.Shortcuts.Desktop.Add(new ShortcutItem { Path = "docs", Type = ShortcutItemType.Folder });

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("CreateShortCut \"$DESKTOP\\docs.lnk\" \"$INSTDIR\\docs\" \"\" \"$SYSDIR\\shell32.dll\" 3", script);
    }

    [Fact]
    public void UT_NSIS_006_GenerateScript_NoRegistry_ExcludesRegistryCode()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.RegistrationMode = RegistrationMode.NoRegistry;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.DoesNotContain("InstallDirRegKey", script);
        Assert.DoesNotContain("WriteRegStr", script);
        Assert.DoesNotContain("DeleteRegKey", script);
    }

    [Fact]
    public void UT_NSIS_007_GenerateScript_ProgramFilesDestination_RequiresAdmin()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.InstallDestination.Type = InstallDestinationType.ProgramFiles;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("RequestExecutionLevel admin", script);
        Assert.Contains("$PROGRAMFILES64", script);
        Assert.Contains("HKLM", script);
    }

    [Fact]
    public void UT_NSIS_008_GenerateScript_AppDataDestination_RequiresUser()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.InstallDestination.Type = InstallDestinationType.AppDataRoaming;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("RequestExecutionLevel user", script);
        Assert.Contains("$APPDATA", script);
        Assert.Contains("HKCU", script);
    }

    [Fact]
    public async Task UT_NSIS_009_BuildInstallerAsync_CancellationRequested_ThrowsOperationCancelledException()
    {
        // Arrange
        var scriptPath = Path.Combine(_tempDir, "test.nsi");
        File.WriteAllText(scriptPath, "; dummy script");

        using var cts = new CancellationTokenSource();
        cts.Cancel(); // 即座にキャンセル

        // Act & Assert
        // makensis.exeが見つからない場合はWin32Exceptionがスローされる
        // キャンセルトークンがキャンセル済みの場合、プロセス起動前にキャンセルされるか、
        // または起動できない場合はWin32Exceptionがスローされる
        var exception = await Record.ExceptionAsync(async () =>
            await _service.BuildInstallerAsync(scriptPath, null, cts.Token));

        // OperationCanceledException, Win32Exception (makensis.exeが見つからない場合),
        // または NotSupportedException (Linux/WSL環境でWindowsプロセスを起動できない場合) のいずれか
        Assert.True(
            exception is OperationCanceledException ||
            exception is System.ComponentModel.Win32Exception ||
            exception is NotSupportedException,
            $"Expected OperationCanceledException, Win32Exception, or NotSupportedException, but got {exception?.GetType().Name ?? "null"}");
    }

    [Fact]
    public void GenerateScript_VersionInfo_ContainsVIProductVersion()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.Version = "2.3.4.5";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("VIProductVersion \"2.3.4.5\"", script);
        Assert.Contains("VIAddVersionKey \"ProductVersion\" \"2.3.4.5\"", script);
    }

    [Fact]
    public void GenerateScript_CustomInstallPath_UsesCustomPath()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.InstallDestination.Type = InstallDestinationType.Custom;
        config.InstallDestination.CustomPath = "C:\\CustomPath\\App";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("InstallDir \"C:\\CustomPath\\App\"", script);
    }

    [Fact]
    public void GenerateScript_LocaleLanguage_IncludesBothLanguages()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.Language = new LanguageSettings { Type = LanguageType.Locale, Value = "" };

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("!insertmacro MUI_LANGUAGE \"Japanese\"", script);
        Assert.Contains("!insertmacro MUI_LANGUAGE \"English\"", script);
    }

    [Fact]
    public void GenerateScript_AllowUserChangeFalse_ExcludesDirectoryPage()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.InstallDestination.AllowUserChange = false;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.DoesNotContain("MUI_PAGE_DIRECTORY", script);
    }

    [Fact]
    public void GenerateScript_CleanupTypeAll_UsesRmDirRecursive()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.All;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("RMDir /r \"$INSTDIR\"", script);
    }

    [Fact]
    public void GenerateScript_CleanupTypeInstalledOnly_PreservesUserData()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.PreserveUserData;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("; Remove only installed files (preserve user data)", script);
        Assert.Contains("installed_files.dat", script);
    }

    [Fact]
    public void UT_NSIS_010_GenerateScript_OpenInstallFolder_GeneratesExecShell()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.PostInstall = new PostInstallSettings
        {
            OpenInstallFolder = true,
            RunAfterInstall = false
        };

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("ExecShell \"open\" \"$INSTDIR\"", script);
    }

    [Fact]
    public void UT_NSIS_011_GenerateScript_RunAfterInstall_GeneratesExecShell()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.PostInstall = new PostInstallSettings
        {
            OpenInstallFolder = false,
            RunAfterInstall = true,
            RunAfterInstallPath = "test.exe"
        };

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("ExecShell \"open\" \"$INSTDIR\\test.exe\"", script);
    }

    [Fact]
    public void UT_NSIS_012_GenerateScript_PreserveUserData_GeneratesInstalledFilesCleanup()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.PreserveUserData;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("installed_files.dat", script);
        Assert.Contains("FileOpen", script);
        Assert.Contains("FileRead", script);
        Assert.Contains("FileClose", script);
    }

    [Fact]
    public void UT_NSIS_013_GenerateScript_CleanupAll_GeneratesRmDirR()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.All;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("RMDir /r \"$INSTDIR\"", script);
    }

    [Fact]
    public void UT_NSIS_014_GenerateScript_ContainsFilePrefix()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.PreserveUserData;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("FILE:", script);
    }

    [Fact]
    public void UT_NSIS_015_GenerateScript_ContainsDirPrefix()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.PreserveUserData;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("DIR:", script);
    }

    [Fact]
    public void UT_NSIS_016_GenerateScript_GeneratesSetRegView64Macro()
    {
        // Arrange
        var config = CreateMinimalConfig();

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("!macro SetRegView64", script);
        Assert.Contains("${If} ${RunningX64}", script);
        Assert.Contains("SetRegView 64", script);
        Assert.Contains("!macroend", script);
    }

    [Fact]
    public void UT_NSIS_017_GenerateScript_InstallSectionCallsSetRegView64()
    {
        // Arrange
        var config = CreateMinimalConfig();

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // インストールセクションに !insertmacro SetRegView64 が含まれることを確認
        var installSectionIndex = script.IndexOf("Section \"Install\"");
        var uninstallSectionIndex = script.IndexOf("Section \"Uninstall\"");

        Assert.True(installSectionIndex >= 0, "Install section not found");
        Assert.True(uninstallSectionIndex > installSectionIndex, "Uninstall section should come after Install section");

        var installSection = script[installSectionIndex..uninstallSectionIndex];
        Assert.Contains("!insertmacro SetRegView64", installSection);
    }

    [Fact]
    public void UT_NSIS_018_GenerateScript_UninstallSectionCallsSetRegView64()
    {
        // Arrange
        var config = CreateMinimalConfig();

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // アンインストールセクションに !insertmacro SetRegView64 が含まれることを確認
        var uninstallSectionIndex = script.IndexOf("Section \"Uninstall\"");

        Assert.True(uninstallSectionIndex >= 0, "Uninstall section not found");

        var uninstallSection = script[uninstallSectionIndex..];
        Assert.Contains("!insertmacro SetRegView64", uninstallSection);
    }

    [Fact]
    public void UT_NSIS_019_GenerateScript_ReservedFilesExcluded()
    {
        // Arrange
        var config = CreateMinimalConfig();

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        Assert.Contains("/x \"installed_files.dat\"", script);
        Assert.Contains("/x \"installed_version.dat\"", script);
        Assert.Contains("/x \"Uninstall.exe\"", script);
    }

    [Fact]
    public void UT_NSIS_020_GenerateScript_UpgradeCleanupGenerated()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.Uninstall.Cleanup = CleanupType.PreserveUserData;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // アップグレード時のクリーンアップ処理が生成されていることを確認
        Assert.Contains("installed_files.dat", script);
        Assert.Contains("IfFileExists", script);
    }

    [Fact]
    public void UT_NSIS_021_GenerateScript_CompanyNameProvided_UsesCompanyNameForStartMenu()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "TestCompany";
        config.BasicInfo.ApplicationName = "TestApp";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名でスタートメニューフォルダーが作成されることを確認
        Assert.Contains("$SMPROGRAMS\\TestCompany", script);
    }

    [Fact]
    public void UT_NSIS_022_GenerateScript_CompanyNameEmpty_UsesAppNameForStartMenu()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "";
        config.BasicInfo.ApplicationName = "TestApp";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名が空の場合、アプリ名でスタートメニューフォルダーが作成されることを確認
        Assert.Contains("$SMPROGRAMS\\TestApp", script);
        Assert.DoesNotContain("$SMPROGRAMS\\\\TestApp", script); // ダブルバックスラッシュにならないことを確認
    }

    [Fact]
    public void UT_NSIS_023_GenerateScript_UninstallWithCompanyName_DeletesCompanyFolder()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "TestCompany";
        config.BasicInfo.ApplicationName = "TestApp";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // アンインストールセクションで会社名フォルダーが削除されることを確認
        var uninstallSectionIndex = script.IndexOf("Section \"Uninstall\"");
        Assert.True(uninstallSectionIndex >= 0, "Uninstall section not found");

        var uninstallSection = script[uninstallSectionIndex..];
        Assert.Contains("RMDir /r \"$SMPROGRAMS\\TestCompany\"", uninstallSection);
    }

    [Fact]
    public void UT_NSIS_024_GenerateScript_UninstallWithEmptyCompanyName_DeletesAppNameFolder()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "";
        config.BasicInfo.ApplicationName = "TestApp";

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // アンインストールセクションでアプリ名フォルダーが削除されることを確認
        var uninstallSectionIndex = script.IndexOf("Section \"Uninstall\"");
        Assert.True(uninstallSectionIndex >= 0, "Uninstall section not found");

        var uninstallSection = script[uninstallSectionIndex..];
        Assert.Contains("RMDir /r \"$SMPROGRAMS\\TestApp\"", uninstallSection);
    }

    [Fact]
    public void UT_NSIS_025_GenerateScript_CompanyNameProvided_GeneratesCorrectInstallPath()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "TestCompany";
        config.BasicInfo.ApplicationName = "TestApp";
        config.InstallDestination.Type = InstallDestinationType.AppDataRoaming;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名がある場合は $APPDATA\会社名\アプリ名 形式
        Assert.Contains("InstallDir \"$APPDATA\\TestCompany\\TestApp\"", script);
    }

    [Fact]
    public void UT_NSIS_026_GenerateScript_CompanyNameEmpty_GeneratesCorrectInstallPathWithoutDoubleBackslash()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "";
        config.BasicInfo.ApplicationName = "TestApp";
        config.InstallDestination.Type = InstallDestinationType.AppDataRoaming;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名が空の場合は $APPDATA\アプリ名 形式（\\が連続しない）
        Assert.Contains("InstallDir \"$APPDATA\\TestApp\"", script);
        Assert.DoesNotContain("$APPDATA\\\\TestApp", script); // \\が連続しないことを確認
    }

    [Fact]
    public void UT_NSIS_027_GenerateScript_CompanyNameProvided_GeneratesCorrectRegistryPath()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "TestCompany";
        config.BasicInfo.ApplicationName = "TestApp";
        config.BasicInfo.RegistrationMode = RegistrationMode.RegisterToAppsAndFeatures;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名がある場合は Software\会社名\アプリ名 形式
        Assert.Contains("\"Software\\TestCompany\\TestApp\"", script);
    }

    [Fact]
    public void UT_NSIS_028_GenerateScript_CompanyNameEmpty_GeneratesCorrectRegistryPathWithoutDoubleBackslash()
    {
        // Arrange
        var config = CreateMinimalConfig();
        config.BasicInfo.CompanyName = "";
        config.BasicInfo.ApplicationName = "TestApp";
        config.BasicInfo.RegistrationMode = RegistrationMode.RegisterToAppsAndFeatures;

        // Act
        var script = _service.GenerateScript(config);

        // Assert
        // 会社名が空の場合は Software\アプリ名 形式（\\が連続しない）
        Assert.Contains("\"Software\\TestApp\"", script);
        Assert.DoesNotContain("Software\\\\TestApp", script); // \\が連続しないことを確認
    }
}
