namespace EasyNSIS.Models;

/// <summary>
/// インストーラーの設定を保持するモデルクラス
/// </summary>
public class InstallerConfig
{
    public BasicInfo BasicInfo { get; set; } = new();
    public SourceFolder SourceFolder { get; set; } = new();
    public InstallDestination InstallDestination { get; set; } = new();
    public ShortcutSettings Shortcuts { get; set; } = new();
    public LicenseSettings License { get; set; } = new();
    public UninstallSettings Uninstall { get; set; } = new();
    public OutputSettings Output { get; set; } = new();
    public PostInstallSettings PostInstall { get; set; } = new();

    public InstallerConfig Clone()
    {
        return new InstallerConfig
        {
            BasicInfo = BasicInfo.Clone(),
            SourceFolder = SourceFolder.Clone(),
            InstallDestination = InstallDestination.Clone(),
            Shortcuts = Shortcuts.Clone(),
            License = License.Clone(),
            Uninstall = Uninstall.Clone(),
            Output = Output.Clone(),
            PostInstall = PostInstall.Clone()
        };
    }

    public bool Equals(InstallerConfig? other)
    {
        if (other is null) return false;
        return BasicInfo.Equals(other.BasicInfo)
            && SourceFolder.Equals(other.SourceFolder)
            && InstallDestination.Equals(other.InstallDestination)
            && Shortcuts.Equals(other.Shortcuts)
            && License.Equals(other.License)
            && Uninstall.Equals(other.Uninstall)
            && Output.Equals(other.Output)
            && PostInstall.Equals(other.PostInstall);
    }
}

/// <summary>
/// 基本情報
/// </summary>
public class BasicInfo
{
    public string CompanyName { get; set; } = string.Empty;
    public string ApplicationName { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public LanguageSettings Language { get; set; } = new();
    public RegistrationMode RegistrationMode { get; set; } = RegistrationMode.RegisterToAppsAndFeatures;
    public string InstallerIcon { get; set; } = string.Empty;

    public BasicInfo Clone()
    {
        return new BasicInfo
        {
            CompanyName = CompanyName,
            ApplicationName = ApplicationName,
            Version = Version,
            Language = Language.Clone(),
            RegistrationMode = RegistrationMode,
            InstallerIcon = InstallerIcon
        };
    }

    public bool Equals(BasicInfo? other)
    {
        if (other is null) return false;
        return CompanyName == other.CompanyName
            && ApplicationName == other.ApplicationName
            && Version == other.Version
            && Language.Equals(other.Language)
            && RegistrationMode == other.RegistrationMode
            && InstallerIcon == other.InstallerIcon;
    }
}

/// <summary>
/// 登録モード
/// </summary>
public enum RegistrationMode
{
    /// <summary>アプリと機能に登録する（デフォルト）</summary>
    RegisterToAppsAndFeatures,
    /// <summary>レジストリを使用しない</summary>
    NoRegistry
}

/// <summary>
/// 言語設定
/// </summary>
public class LanguageSettings
{
    public LanguageType Type { get; set; } = LanguageType.Locale;
    public string Value { get; set; } = "ja-JP";

    public LanguageSettings Clone()
    {
        return new LanguageSettings
        {
            Type = Type,
            Value = Value
        };
    }

    public bool Equals(LanguageSettings? other)
    {
        if (other is null) return false;
        return Type == other.Type && Value == other.Value;
    }
}

public enum LanguageType
{
    Locale,
    Fixed
}

/// <summary>
/// インストール対象フォルダー
/// </summary>
public class SourceFolder
{
    public string Path { get; set; } = string.Empty;

    public SourceFolder Clone()
    {
        return new SourceFolder { Path = Path };
    }

    public bool Equals(SourceFolder? other)
    {
        if (other is null) return false;
        return Path == other.Path;
    }
}

/// <summary>
/// インストール先設定
/// </summary>
public class InstallDestination
{
    public InstallDestinationType Type { get; set; } = InstallDestinationType.AppDataRoaming;
    public string CustomPath { get; set; } = string.Empty;
    public bool AllowUserChange { get; set; } = true;

    public InstallDestination Clone()
    {
        return new InstallDestination
        {
            Type = Type,
            CustomPath = CustomPath,
            AllowUserChange = AllowUserChange
        };
    }

    public bool Equals(InstallDestination? other)
    {
        if (other is null) return false;
        return Type == other.Type
            && CustomPath == other.CustomPath
            && AllowUserChange == other.AllowUserChange;
    }
}

public enum InstallDestinationType
{
    AppDataRoaming,
    AppDataLocal,
    ProgramFiles,
    Custom
}

/// <summary>
/// ショートカット設定
/// </summary>
public class ShortcutSettings
{
    public List<ShortcutItem> Desktop { get; set; } = [];
    public List<ShortcutItem> StartMenu { get; set; } = [];

    public ShortcutSettings Clone()
    {
        return new ShortcutSettings
        {
            Desktop = Desktop.Select(x => x.Clone()).ToList(),
            StartMenu = StartMenu.Select(x => x.Clone()).ToList()
        };
    }

    public bool Equals(ShortcutSettings? other)
    {
        if (other is null) return false;
        return Desktop.SequenceEqual(other.Desktop)
            && StartMenu.SequenceEqual(other.StartMenu);
    }
}

public class ShortcutItem : IEquatable<ShortcutItem>
{
    public ShortcutItemType Type { get; set; }
    public string Path { get; set; } = string.Empty;

    public ShortcutItem Clone()
    {
        return new ShortcutItem
        {
            Type = Type,
            Path = Path
        };
    }

    public bool Equals(ShortcutItem? other)
    {
        if (other is null) return false;
        return Type == other.Type && Path == other.Path;
    }

    public override bool Equals(object? obj) => Equals(obj as ShortcutItem);
    public override int GetHashCode() => HashCode.Combine(Type, Path);
}

public enum ShortcutItemType
{
    File,
    Folder
}

/// <summary>
/// ライセンス設定
/// </summary>
public class LicenseSettings
{
    public LicenseType Type { get; set; } = LicenseType.Text;
    public string FilePath { get; set; } = string.Empty;
    public string Text { get; set; } = string.Empty;

    public LicenseSettings Clone()
    {
        return new LicenseSettings
        {
            Type = Type,
            FilePath = FilePath,
            Text = Text
        };
    }

    public bool Equals(LicenseSettings? other)
    {
        if (other is null) return false;
        return Type == other.Type
            && FilePath == other.FilePath
            && Text == other.Text;
    }
}

public enum LicenseType
{
    Default,
    File,
    Text
}

/// <summary>
/// アンインストール設定
/// </summary>
public class UninstallSettings
{
    public CleanupType Cleanup { get; set; } = CleanupType.PreserveUserData;

    public UninstallSettings Clone()
    {
        return new UninstallSettings { Cleanup = Cleanup };
    }

    public bool Equals(UninstallSettings? other)
    {
        if (other is null) return false;
        return Cleanup == other.Cleanup;
    }
}

public enum CleanupType
{
    All,
    PreserveUserData
}

/// <summary>
/// 出力設定
/// </summary>
public class OutputSettings
{
    public string Folder { get; set; } = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);

    public OutputSettings Clone()
    {
        return new OutputSettings { Folder = Folder };
    }

    public bool Equals(OutputSettings? other)
    {
        if (other is null) return false;
        return Folder == other.Folder;
    }
}

/// <summary>
/// インストール後設定
/// </summary>
public class PostInstallSettings
{
    public bool OpenInstallFolder { get; set; } = false;
    public bool RunAfterInstall { get; set; } = false;
    public string RunAfterInstallPath { get; set; } = string.Empty;

    public PostInstallSettings Clone()
    {
        return new PostInstallSettings
        {
            OpenInstallFolder = OpenInstallFolder,
            RunAfterInstall = RunAfterInstall,
            RunAfterInstallPath = RunAfterInstallPath
        };
    }

    public bool Equals(PostInstallSettings? other)
    {
        if (other is null) return false;
        return OpenInstallFolder == other.OpenInstallFolder
            && RunAfterInstall == other.RunAfterInstall
            && RunAfterInstallPath == other.RunAfterInstallPath;
    }
}
