using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using EasyNSIS.Models;
using EasyNSIS.Services;
using Microsoft.Win32;

namespace EasyNSIS.ViewModels;

public class BuildResultEventArgs : EventArgs
{
    public bool Success { get; }
    public string Message { get; }
    public string? OutputPath { get; }

    public BuildResultEventArgs(bool success, string message, string? outputPath = null)
    {
        Success = success;
        Message = message;
        OutputPath = outputPath;
    }
}

public partial class MainViewModel : ObservableObject
{
    public event EventHandler<BuildResultEventArgs>? BuildResultReady;
    private readonly IConfigurationService _configService;
    private readonly IValidationService _validationService;
    private readonly INsisService _nsisService;
    private readonly ILogService _logService;

    private InstallerConfig _savedConfig;
    private string? _currentFilePath;
    private CancellationTokenSource? _buildCts;

    [ObservableProperty]
    private int _selectedTabIndex;

    [ObservableProperty]
    private BuildState _buildState = BuildState.Idle;

    [ObservableProperty]
    private string _buildLog = string.Empty;

    [ObservableProperty]
    private string _windowTitle = Resources.Strings.AppTitle_New;

    // Basic Info
    [ObservableProperty]
    private string _companyName = string.Empty;

    [ObservableProperty]
    private string _applicationName = string.Empty;

    // Version (4 segments)
    [ObservableProperty]
    private string _versionMajor = "1";

    [ObservableProperty]
    private string _versionMinor = "0";

    [ObservableProperty]
    private string _versionBuild = "0";

    [ObservableProperty]
    private string _versionRevision = "0";

    public string Version => $"{VersionMajor}.{VersionMinor}.{VersionBuild}.{VersionRevision}";

    [ObservableProperty]
    private bool _useLocale = true;

    [ObservableProperty]
    private string _selectedLanguage = GetDefaultLanguage();

    /// <summary>
    /// OSロケールに基づいてデフォルト言語を決定する
    /// ja-JPの場合は日本語、それ以外は英語
    /// </summary>
    private static string GetDefaultLanguage()
    {
        var cultureName = CultureInfo.CurrentCulture.Name;
        return cultureName == "ja-JP" ? "ja-JP" : "en-US";
    }

    [ObservableProperty]
    private RegistrationMode _registrationMode = RegistrationMode.RegisterToAppsAndFeatures;

    [ObservableProperty]
    private string _installerIcon = string.Empty;

    // Source Folder
    [ObservableProperty]
    private string _sourceFolderPath = string.Empty;

    // Install Destination
    [ObservableProperty]
    private InstallDestinationType _installDestinationType = InstallDestinationType.AppDataRoaming;

    [ObservableProperty]
    private string _customInstallPath = string.Empty;

    [ObservableProperty]
    private bool _allowUserChangeInstallPath = true;

    // Shortcuts
    public ObservableCollection<ShortcutItem> DesktopShortcuts { get; } = [];
    public ObservableCollection<ShortcutItem> StartMenuShortcuts { get; } = [];

    [ObservableProperty]
    private ShortcutItem? _selectedDesktopShortcut;

    [ObservableProperty]
    private ShortcutItem? _selectedStartMenuShortcut;

    // License
    [ObservableProperty]
    private LicenseType _licenseType = LicenseType.Text;

    [ObservableProperty]
    private string _licenseFilePath = string.Empty;

    [ObservableProperty]
    private string _licenseText = string.Empty;

    // Output
    [ObservableProperty]
    private string _outputFolder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    [ObservableProperty]
    private CleanupType _cleanupType = CleanupType.PreserveUserData;

    // PostInstall
    [ObservableProperty]
    private bool _openInstallFolderAfterInstall;

    [ObservableProperty]
    private bool _runAfterInstall;

    [ObservableProperty]
    private string _runAfterInstallPath = string.Empty;

    [ObservableProperty]
    private string? _runAfterInstallPathError;

    // Validation Errors
    [ObservableProperty]
    private string? _companyNameError;

    [ObservableProperty]
    private string? _applicationNameError;

    [ObservableProperty]
    private string? _versionError;

    [ObservableProperty]
    private string? _sourceFolderError;

    [ObservableProperty]
    private string? _customPathError;

    [ObservableProperty]
    private string? _licenseFileError;

    [ObservableProperty]
    private string? _licenseTextError;

    [ObservableProperty]
    private string? _outputFolderError;

    [ObservableProperty]
    private string? _installerIconError;

    public string[] AvailableLanguages { get; } = ["ja-JP", "en-US"];

    public bool IsBuilding => BuildState == BuildState.Building;
    public bool IsNotBuilding => BuildState != BuildState.Building;

    public bool ShowNoRegistryWarning => RegistrationMode == RegistrationMode.NoRegistry;

    public MainViewModel(
        IConfigurationService configService,
        IValidationService validationService,
        INsisService nsisService,
        ILogService logService)
    {
        _configService = configService;
        _validationService = validationService;
        _nsisService = nsisService;
        _logService = logService;

        _savedConfig = _configService.CreateDefault();
        LoadFromConfig(_savedConfig);
    }

    partial void OnRegistrationModeChanged(RegistrationMode value)
    {
        OnPropertyChanged(nameof(ShowNoRegistryWarning));
    }

    partial void OnBuildStateChanged(BuildState value)
    {
        OnPropertyChanged(nameof(IsBuilding));
        OnPropertyChanged(nameof(IsNotBuilding));
    }

    public bool HasUnsavedChanges()
    {
        var currentConfig = CreateConfigFromViewModel();
        return !currentConfig.Equals(_savedConfig);
    }

    private InstallerConfig CreateConfigFromViewModel()
    {
        return new InstallerConfig
        {
            BasicInfo = new BasicInfo
            {
                CompanyName = CompanyName,
                ApplicationName = ApplicationName,
                Version = Version,
                Language = new LanguageSettings
                {
                    Type = UseLocale ? LanguageType.Locale : LanguageType.Fixed,
                    Value = SelectedLanguage
                },
                RegistrationMode = RegistrationMode,
                InstallerIcon = InstallerIcon
            },
            SourceFolder = new SourceFolder { Path = SourceFolderPath },
            InstallDestination = new InstallDestination
            {
                Type = InstallDestinationType,
                CustomPath = CustomInstallPath,
                AllowUserChange = AllowUserChangeInstallPath
            },
            Shortcuts = new ShortcutSettings
            {
                Desktop = [.. DesktopShortcuts],
                StartMenu = [.. StartMenuShortcuts]
            },
            License = new LicenseSettings
            {
                Type = LicenseType,
                FilePath = LicenseFilePath,
                Text = LicenseText
            },
            Uninstall = new UninstallSettings { Cleanup = CleanupType },
            Output = new OutputSettings { Folder = OutputFolder },
            PostInstall = new PostInstallSettings
            {
                OpenInstallFolder = OpenInstallFolderAfterInstall,
                RunAfterInstall = RunAfterInstall,
                RunAfterInstallPath = RunAfterInstallPath
            }
        };
    }

    private void LoadFromConfig(InstallerConfig config)
    {
        CompanyName = config.BasicInfo.CompanyName;
        ApplicationName = config.BasicInfo.ApplicationName;

        // Parse version into 4 segments
        var versionParts = (config.BasicInfo.Version ?? "1.0.0.0").Split('.');
        VersionMajor = versionParts.Length > 0 ? versionParts[0] : "1";
        VersionMinor = versionParts.Length > 1 ? versionParts[1] : "0";
        VersionBuild = versionParts.Length > 2 ? versionParts[2] : "0";
        VersionRevision = versionParts.Length > 3 ? versionParts[3] : "0";

        UseLocale = config.BasicInfo.Language.Type == LanguageType.Locale;
        SelectedLanguage = config.BasicInfo.Language.Value;
        RegistrationMode = config.BasicInfo.RegistrationMode;
        InstallerIcon = config.BasicInfo.InstallerIcon;

        SourceFolderPath = config.SourceFolder.Path;

        InstallDestinationType = config.InstallDestination.Type;
        CustomInstallPath = config.InstallDestination.CustomPath;
        AllowUserChangeInstallPath = config.InstallDestination.AllowUserChange;

        DesktopShortcuts.Clear();
        foreach (var item in config.Shortcuts.Desktop)
        {
            DesktopShortcuts.Add(item);
        }

        StartMenuShortcuts.Clear();
        foreach (var item in config.Shortcuts.StartMenu)
        {
            StartMenuShortcuts.Add(item);
        }

        LicenseType = config.License.Type;
        LicenseFilePath = config.License.FilePath;
        LicenseText = config.License.Text;

        CleanupType = config.Uninstall.Cleanup;
        OutputFolder = config.Output.Folder;

        OpenInstallFolderAfterInstall = config.PostInstall.OpenInstallFolder;
        RunAfterInstall = config.PostInstall.RunAfterInstall;
        RunAfterInstallPath = config.PostInstall.RunAfterInstallPath;

        ClearAllErrors();
    }

    private void ClearAllErrors()
    {
        CompanyNameError = null;
        ApplicationNameError = null;
        VersionError = null;
        SourceFolderError = null;
        CustomPathError = null;
        LicenseFileError = null;
        LicenseTextError = null;
        OutputFolderError = null;
        InstallerIconError = null;
        RunAfterInstallPathError = null;
    }

    private void UpdateWindowTitle()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            WindowTitle = Resources.Strings.AppTitle_New;
        }
        else
        {
            WindowTitle = string.Format(Resources.Strings.AppTitle_Format, Path.GetFileName(_currentFilePath));
        }
    }

    [RelayCommand]
    private void BrowseSourceFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = Resources.Strings.Label_SourceFolderPath
        };

        if (dialog.ShowDialog() == true)
        {
            SourceFolderPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseCustomInstallPath()
    {
        var dialog = new OpenFolderDialog
        {
            Title = Resources.Strings.Label_InstallDestination
        };

        if (dialog.ShowDialog() == true)
        {
            CustomInstallPath = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseInstallerIcon()
    {
        var dialog = new OpenFileDialog
        {
            Filter = Resources.Strings.Filter_IcoFiles,
            Title = Resources.Strings.Label_InstallerIcon
        };

        if (dialog.ShowDialog() == true)
        {
            InstallerIcon = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseLicenseFile()
    {
        var dialog = new OpenFileDialog
        {
            Filter = Resources.Strings.Filter_TxtFiles,
            Title = Resources.Strings.Label_LicenseType
        };

        if (dialog.ShowDialog() == true)
        {
            LicenseFilePath = dialog.FileName;
        }
    }

    [RelayCommand]
    private void BrowseOutputFolder()
    {
        var dialog = new OpenFolderDialog
        {
            Title = Resources.Strings.Label_OutputFolder
        };

        if (dialog.ShowDialog() == true)
        {
            OutputFolder = dialog.FolderName;
        }
    }

    [RelayCommand]
    private void BrowseRunAfterInstallPath()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show(Resources.Strings.Err_FolderNotFound, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            InitialDirectory = SourceFolderPath,
            Filter = Resources.Strings.Filter_ExecutableFiles,
            Title = Resources.Strings.Label_RunAfterInstallPath
        };

        if (dialog.ShowDialog() == true)
        {
            var relativePath = Path.GetRelativePath(SourceFolderPath, dialog.FileName);
            if (!relativePath.StartsWith(".."))
            {
                RunAfterInstallPath = relativePath;
            }
        }
    }

    [RelayCommand]
    private void AddDesktopFileShortcut()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show(Resources.Strings.Err_FolderNotFound, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            InitialDirectory = SourceFolderPath,
            Filter = Resources.Strings.Filter_AllFiles
        };

        if (dialog.ShowDialog() == true)
        {
            var relativePath = Path.GetRelativePath(SourceFolderPath, dialog.FileName);
            if (!relativePath.StartsWith(".."))
            {
                DesktopShortcuts.Add(new ShortcutItem { Type = ShortcutItemType.File, Path = relativePath });
            }
        }
    }

    [RelayCommand]
    private void AddDesktopFolderShortcut()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show(Resources.Strings.Err_FolderNotFound, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFolderDialog
        {
            InitialDirectory = SourceFolderPath
        };

        if (dialog.ShowDialog() == true)
        {
            var relativePath = Path.GetRelativePath(SourceFolderPath, dialog.FolderName);
            if (!relativePath.StartsWith(".."))
            {
                DesktopShortcuts.Add(new ShortcutItem { Type = ShortcutItemType.Folder, Path = relativePath });
            }
        }
    }

    [RelayCommand]
    private void RemoveDesktopShortcut()
    {
        if (SelectedDesktopShortcut != null)
        {
            DesktopShortcuts.Remove(SelectedDesktopShortcut);
        }
    }

    [RelayCommand]
    private void AddStartMenuFileShortcut()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show(Resources.Strings.Err_FolderNotFound, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFileDialog
        {
            InitialDirectory = SourceFolderPath,
            Filter = Resources.Strings.Filter_AllFiles
        };

        if (dialog.ShowDialog() == true)
        {
            var relativePath = Path.GetRelativePath(SourceFolderPath, dialog.FileName);
            if (!relativePath.StartsWith(".."))
            {
                StartMenuShortcuts.Add(new ShortcutItem { Type = ShortcutItemType.File, Path = relativePath });
            }
        }
    }

    [RelayCommand]
    private void AddStartMenuFolderShortcut()
    {
        if (string.IsNullOrEmpty(SourceFolderPath) || !Directory.Exists(SourceFolderPath))
        {
            MessageBox.Show(Resources.Strings.Err_FolderNotFound, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new OpenFolderDialog
        {
            InitialDirectory = SourceFolderPath
        };

        if (dialog.ShowDialog() == true)
        {
            var relativePath = Path.GetRelativePath(SourceFolderPath, dialog.FolderName);
            if (!relativePath.StartsWith(".."))
            {
                StartMenuShortcuts.Add(new ShortcutItem { Type = ShortcutItemType.Folder, Path = relativePath });
            }
        }
    }

    [RelayCommand]
    private void RemoveStartMenuShortcut()
    {
        if (SelectedStartMenuShortcut != null)
        {
            StartMenuShortcuts.Remove(SelectedStartMenuShortcut);
        }
    }

    [RelayCommand]
    private void LoadConfig()
    {
        var dialog = new OpenFileDialog
        {
            Filter = Resources.Strings.Filter_XmlFiles,
            Title = Resources.Strings.Btn_Load
        };

        if (dialog.ShowDialog() == true)
        {
            try
            {
                var config = _configService.LoadFromFile(dialog.FileName);
                LoadFromConfig(config);
                _savedConfig = config.Clone();
                _currentFilePath = dialog.FileName;
                UpdateWindowTitle();
            }
            catch (Exception ex)
            {
                _logService.LogError("Failed to load config", ex);
                MessageBox.Show(
                    string.Format(Resources.Strings.Err_LoadConfigFailed, ex.Message),
                    Resources.Strings.AppTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
            }
        }
    }

    [RelayCommand]
    private void SaveConfig()
    {
        if (string.IsNullOrEmpty(_currentFilePath))
        {
            SaveConfigAs();
            return;
        }

        SaveConfigToFile(_currentFilePath);
    }

    [RelayCommand]
    private void SaveConfigAs()
    {
        var dialog = new SaveFileDialog
        {
            Filter = Resources.Strings.Filter_XmlFiles,
            Title = Resources.Strings.Btn_SaveAs,
            DefaultExt = ".xml"
        };

        if (dialog.ShowDialog() == true)
        {
            SaveConfigToFile(dialog.FileName);
        }
    }

    private void SaveConfigToFile(string filePath)
    {
        try
        {
            var config = CreateConfigFromViewModel();
            _configService.SaveToFile(config, filePath);
            _savedConfig = config.Clone();
            _currentFilePath = filePath;
            UpdateWindowTitle();
        }
        catch (Exception ex)
        {
            _logService.LogError("Failed to save config", ex);
            MessageBox.Show(
                string.Format(Resources.Strings.Err_SaveConfigFailed, ex.Message),
                Resources.Strings.AppTitle,
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }

    [RelayCommand]
    private async Task BuildOrInterruptAsync()
    {
        if (BuildState == BuildState.Building)
        {
            _buildCts?.Cancel();
            return;
        }

        // Validate
        var config = CreateConfigFromViewModel();
        var validationResult = _validationService.ValidateAll(config);

        if (!validationResult.IsValid)
        {
            var errorList = string.Join("\n", validationResult.Errors.Select(e => $"- {e.Message}"));
            MessageBox.Show(
                errorList,
                Resources.Strings.Err_ValidationFailed,
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
            return;
        }

        // Validate NSIS
        if (!_nsisService.ValidateNsisInstallation(out var nsisError))
        {
            MessageBox.Show(nsisError, Resources.Strings.AppTitle, MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }

        // Create output folder if needed
        if (!Directory.Exists(OutputFolder))
        {
            try
            {
                Directory.CreateDirectory(OutputFolder);
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    string.Format(Resources.Strings.Err_SaveConfigFailed, ex.Message),
                    Resources.Strings.AppTitle,
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                return;
            }
        }

        BuildState = BuildState.Building;
        BuildLog = string.Empty;
        _buildCts = new CancellationTokenSource();

        try
        {
            // Generate script
            var script = _nsisService.GenerateScript(config);
            var scriptPath = Path.Combine(OutputFolder, $"{ApplicationName}_Setup.nsi");
            // NSIS requires UTF-8 with BOM for Unicode scripts
            await File.WriteAllTextAsync(scriptPath, script, new System.Text.UTF8Encoding(true), _buildCts.Token);

            _logService.LogBuild($"Script generated: {scriptPath}");
            BuildLog += $"Script generated: {scriptPath}\n";

            // Build
            var progress = new Progress<string>(line =>
            {
                BuildLog += line + "\n";
                _logService.LogBuild(line);
            });

            var (success, output) = await _nsisService.BuildInstallerAsync(scriptPath, progress, _buildCts.Token);

            if (success)
            {
                BuildState = BuildState.Completed;
                BuildLog += "\n" + Resources.Strings.Msg_BuildSuccess + "\n";
                BuildResultReady?.Invoke(this, new BuildResultEventArgs(true, Resources.Strings.Msg_BuildSuccess, OutputFolder));
            }
            else
            {
                BuildState = BuildState.Failed;
                BuildLog += "\n" + Resources.Strings.Msg_BuildFailed + "\n";
                BuildResultReady?.Invoke(this, new BuildResultEventArgs(false, Resources.Strings.Msg_BuildFailed));
            }
        }
        catch (OperationCanceledException)
        {
            BuildState = BuildState.Cancelled;
            BuildLog += "\n" + Resources.Strings.Msg_BuildCancelled + "\n";
            _logService.LogBuild("Build cancelled by user");
        }
        catch (Exception ex)
        {
            BuildState = BuildState.Failed;
            BuildLog += $"\nError: {ex.Message}\n";
            _logService.LogError("Build failed", ex);
            BuildResultReady?.Invoke(this, new BuildResultEventArgs(false, $"{Resources.Strings.Msg_BuildFailed}\n{ex.Message}"));
        }
        finally
        {
            _buildCts?.Dispose();
            _buildCts = null;
            if (BuildState == BuildState.Building)
            {
                BuildState = BuildState.Idle;
            }
        }
    }

    public bool CanClose()
    {
        if (BuildState == BuildState.Building)
        {
            var result = MessageBox.Show(
                Resources.Strings.Msg_BuildInProgress,
                Resources.Strings.AppTitle,
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
            {
                _buildCts?.Cancel();
                return true;
            }
            return false;
        }

        if (HasUnsavedChanges())
        {
            var result = MessageBox.Show(
                Resources.Strings.Msg_UnsavedChanges,
                Resources.Strings.Msg_UnsavedTitle,
                MessageBoxButton.YesNoCancel,
                MessageBoxImage.Question);

            switch (result)
            {
                case MessageBoxResult.Yes:
                    SaveConfig();
                    return true;
                case MessageBoxResult.No:
                    return true;
                default:
                    return false;
            }
        }

        return true;
    }
}
