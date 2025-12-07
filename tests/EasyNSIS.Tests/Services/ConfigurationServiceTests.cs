using EasyNSIS.Models;
using EasyNSIS.Services;

namespace EasyNSIS.Tests.Services;

public class ConfigurationServiceTests : IDisposable
{
    private readonly ConfigurationService _service = new();
    private readonly string _tempDir;
    private readonly List<string> _tempFiles = [];

    public ConfigurationServiceTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"EasyNSIS_Tests_{Guid.NewGuid()}");
        Directory.CreateDirectory(_tempDir);
    }

    public void Dispose()
    {
        foreach (var file in _tempFiles)
        {
            if (File.Exists(file))
                File.Delete(file);
        }
        if (Directory.Exists(_tempDir))
            Directory.Delete(_tempDir, true);
    }

    private string CreateTempFile(string content, string extension = ".xml")
    {
        var path = Path.Combine(_tempDir, $"test_{Guid.NewGuid()}{extension}");
        File.WriteAllText(path, content);
        _tempFiles.Add(path);
        return path;
    }

    #region Save/Load Tests

    [Fact]
    public void UT_CFG_001_Save_ValidConfig_CreatesXmlFile()
    {
        // Arrange
        var config = _service.CreateDefault();
        config.BasicInfo.CompanyName = "TestCompany";
        config.BasicInfo.ApplicationName = "TestApp";
        var filePath = Path.Combine(_tempDir, "test_config.xml");
        _tempFiles.Add(filePath);

        // Act
        _service.SaveToFile(config, filePath);

        // Assert
        Assert.True(File.Exists(filePath));
        var content = File.ReadAllText(filePath);
        Assert.Contains("EasyNsisConfig", content);
        Assert.Contains("TestCompany", content);
        Assert.Contains("TestApp", content);
    }

    [Fact]
    public void UT_CFG_002_Load_SavedFile_RestoresConfig()
    {
        // Arrange
        var originalConfig = _service.CreateDefault();
        originalConfig.BasicInfo.CompanyName = "TestCompany";
        originalConfig.BasicInfo.ApplicationName = "TestApp";
        originalConfig.BasicInfo.Version = "2.0.0.0";
        originalConfig.InstallDestination.Type = InstallDestinationType.ProgramFiles;

        var filePath = Path.Combine(_tempDir, "test_config.xml");
        _tempFiles.Add(filePath);
        _service.SaveToFile(originalConfig, filePath);

        // Act
        var loadedConfig = _service.LoadFromFile(filePath);

        // Assert
        Assert.Equal("TestCompany", loadedConfig.BasicInfo.CompanyName);
        Assert.Equal("TestApp", loadedConfig.BasicInfo.ApplicationName);
        Assert.Equal("2.0.0.0", loadedConfig.BasicInfo.Version);
        Assert.Equal(InstallDestinationType.ProgramFiles, loadedConfig.InstallDestination.Type);
    }

    [Fact]
    public void UT_CFG_003_Load_NonExistentFile_ThrowsException()
    {
        // Arrange
        var filePath = Path.Combine(_tempDir, "non_existent.xml");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _service.LoadFromFile(filePath));
    }

    [Fact]
    public void UT_CFG_004_Load_InvalidXml_ThrowsException()
    {
        // Arrange
        var filePath = CreateTempFile("This is not valid XML <>");

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _service.LoadFromFile(filePath));
    }

    [Fact]
    public void UT_CFG_005_CreateDefault_ReturnsValidDefaults()
    {
        // Act
        var config = _service.CreateDefault();

        // Assert
        Assert.NotNull(config);
        Assert.Equal("1.0.0.0", config.BasicInfo.Version);
        Assert.Equal(InstallDestinationType.AppDataRoaming, config.InstallDestination.Type);
        Assert.True(config.InstallDestination.AllowUserChange);
        Assert.Equal(RegistrationMode.RegisterToAppsAndFeatures, config.BasicInfo.RegistrationMode);
        Assert.Equal(CleanupType.PreserveUserData, config.Uninstall.Cleanup);
        Assert.NotEmpty(config.Output.Folder);
    }

    [Fact]
    public void UT_CFG_006_Save_JapaneseContent_SavesAsUtf8()
    {
        // Arrange
        var config = _service.CreateDefault();
        config.BasicInfo.CompanyName = "テスト会社";
        config.BasicInfo.ApplicationName = "テストアプリ";
        var filePath = Path.Combine(_tempDir, "test_japanese.xml");
        _tempFiles.Add(filePath);

        // Act
        _service.SaveToFile(config, filePath);

        // Assert
        var content = File.ReadAllText(filePath);
        Assert.Contains("テスト会社", content);
        Assert.Contains("テストアプリ", content);
        Assert.Contains("utf-8", content.ToLower());

        // Verify it can be loaded back
        var loadedConfig = _service.LoadFromFile(filePath);
        Assert.Equal("テスト会社", loadedConfig.BasicInfo.CompanyName);
        Assert.Equal("テストアプリ", loadedConfig.BasicInfo.ApplicationName);
    }

    #endregion

    #region Additional Load Tests

    [Fact]
    public void Load_InvalidRootElement_ThrowsException()
    {
        // Arrange
        var xml = @"<?xml version=""1.0"" encoding=""utf-8""?>
<WrongRoot version=""1"">
  <BasicInfo>
    <CompanyName>Test</CompanyName>
  </BasicInfo>
</WrongRoot>";
        var filePath = CreateTempFile(xml);

        // Act & Assert
        Assert.ThrowsAny<Exception>(() => _service.LoadFromFile(filePath));
    }

    [Fact]
    public void Load_AllSettings_PreservesAllValues()
    {
        // Arrange
        var config = _service.CreateDefault();
        config.BasicInfo.CompanyName = "Company";
        config.BasicInfo.ApplicationName = "App";
        config.BasicInfo.Version = "1.2.3.4";
        config.BasicInfo.Language.Type = LanguageType.Fixed;
        config.BasicInfo.Language.Value = "en-US";
        config.BasicInfo.RegistrationMode = RegistrationMode.NoRegistry;
        config.SourceFolder.Path = @"C:\Source";
        config.InstallDestination.Type = InstallDestinationType.Custom;
        config.InstallDestination.CustomPath = @"C:\Install";
        config.InstallDestination.AllowUserChange = false;
        config.Shortcuts.Desktop.Add(new ShortcutItem { Type = ShortcutItemType.File, Path = "app.exe" });
        config.Shortcuts.StartMenu.Add(new ShortcutItem { Type = ShortcutItemType.Folder, Path = "docs" });
        config.License.Type = LicenseType.Text;
        config.License.Text = "License text";
        config.Uninstall.Cleanup = CleanupType.All;
        config.Output.Folder = @"C:\Output";

        var filePath = Path.Combine(_tempDir, "full_config.xml");
        _tempFiles.Add(filePath);
        _service.SaveToFile(config, filePath);

        // Act
        var loaded = _service.LoadFromFile(filePath);

        // Assert
        Assert.Equal("Company", loaded.BasicInfo.CompanyName);
        Assert.Equal("App", loaded.BasicInfo.ApplicationName);
        Assert.Equal("1.2.3.4", loaded.BasicInfo.Version);
        Assert.Equal(LanguageType.Fixed, loaded.BasicInfo.Language.Type);
        Assert.Equal("en-US", loaded.BasicInfo.Language.Value);
        Assert.Equal(RegistrationMode.NoRegistry, loaded.BasicInfo.RegistrationMode);
        Assert.Equal(@"C:\Source", loaded.SourceFolder.Path);
        Assert.Equal(InstallDestinationType.Custom, loaded.InstallDestination.Type);
        Assert.Equal(@"C:\Install", loaded.InstallDestination.CustomPath);
        Assert.False(loaded.InstallDestination.AllowUserChange);
        Assert.Single(loaded.Shortcuts.Desktop);
        Assert.Equal(ShortcutItemType.File, loaded.Shortcuts.Desktop[0].Type);
        Assert.Equal("app.exe", loaded.Shortcuts.Desktop[0].Path);
        Assert.Single(loaded.Shortcuts.StartMenu);
        Assert.Equal(ShortcutItemType.Folder, loaded.Shortcuts.StartMenu[0].Type);
        Assert.Equal("docs", loaded.Shortcuts.StartMenu[0].Path);
        Assert.Equal(LicenseType.Text, loaded.License.Type);
        Assert.Equal("License text", loaded.License.Text);
        Assert.Equal(CleanupType.All, loaded.Uninstall.Cleanup);
        Assert.Equal(@"C:\Output", loaded.Output.Folder);
    }

    #endregion
}
