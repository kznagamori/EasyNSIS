using System.IO;
using System.Xml.Linq;
using EasyNSIS.Models;

namespace EasyNSIS.Services;

public class ConfigurationService : IConfigurationService
{
    private const string ConfigVersion = "1";

    public InstallerConfig CreateDefault()
    {
        return new InstallerConfig
        {
            BasicInfo = new BasicInfo
            {
                Version = "1.0.0.0",
                Language = new LanguageSettings
                {
                    Type = LanguageType.Locale,
                    Value = System.Globalization.CultureInfo.CurrentCulture.Name == "ja-JP" ? "ja-JP" : "en-US"
                },
                RegistrationMode = RegistrationMode.RegisterToAppsAndFeatures
            },
            InstallDestination = new InstallDestination
            {
                Type = InstallDestinationType.AppDataRoaming,
                AllowUserChange = true
            },
            License = new LicenseSettings
            {
                Type = LicenseType.Text
            },
            Uninstall = new UninstallSettings
            {
                Cleanup = CleanupType.PreserveUserData
            },
            Output = new OutputSettings
            {
                Folder = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments)
            }
        };
    }

    public InstallerConfig LoadFromFile(string filePath)
    {
        var doc = XDocument.Load(filePath);
        var root = doc.Root ?? throw new InvalidDataException("Invalid XML: no root element");

        if (root.Name.LocalName != "EasyNsisConfig")
        {
            throw new InvalidDataException("Invalid XML: root element must be EasyNsisConfig");
        }

        var config = CreateDefault();
        var basePath = Path.GetDirectoryName(filePath) ?? string.Empty;

        // BasicInfo
        var basicInfo = root.Element("BasicInfo");
        if (basicInfo != null)
        {
            config.BasicInfo.CompanyName = basicInfo.Element("CompanyName")?.Value ?? string.Empty;
            config.BasicInfo.ApplicationName = basicInfo.Element("ApplicationName")?.Value ?? string.Empty;
            config.BasicInfo.Version = basicInfo.Element("Version")?.Value ?? string.Empty;

            var language = basicInfo.Element("Language");
            if (language != null)
            {
                var typeAttr = language.Attribute("type")?.Value;
                config.BasicInfo.Language.Type = typeAttr == "fixed" ? LanguageType.Fixed : LanguageType.Locale;
                config.BasicInfo.Language.Value = language.Attribute("value")?.Value ?? "ja-JP";
            }

            var registrationModeStr = basicInfo.Element("RegistrationMode")?.Value;
            config.BasicInfo.RegistrationMode = registrationModeStr switch
            {
                "NoRegistry" => RegistrationMode.NoRegistry,
                _ => RegistrationMode.RegisterToAppsAndFeatures
            };

            config.BasicInfo.InstallerIcon = ResolvePath(basicInfo.Element("InstallerIcon")?.Value, basePath);
        }

        // SourceFolder
        var sourceFolder = root.Element("SourceFolder");
        if (sourceFolder != null)
        {
            config.SourceFolder.Path = ResolvePath(sourceFolder.Element("Path")?.Value, basePath);
        }

        // InstallDestination
        var installDest = root.Element("InstallDestination");
        if (installDest != null)
        {
            var typeStr = installDest.Element("Type")?.Value ?? "AppDataRoaming";
            config.InstallDestination.Type = typeStr switch
            {
                "AppDataLocal" => InstallDestinationType.AppDataLocal,
                "ProgramFiles" => InstallDestinationType.ProgramFiles,
                "Custom" => InstallDestinationType.Custom,
                _ => InstallDestinationType.AppDataRoaming
            };
            config.InstallDestination.CustomPath = ResolvePath(installDest.Element("CustomPath")?.Value, basePath);
            var allowChangeStr = installDest.Element("AllowUserChange")?.Value;
            config.InstallDestination.AllowUserChange = allowChangeStr?.ToLower() != "false";
        }

        // Shortcuts
        var shortcuts = root.Element("Shortcuts");
        if (shortcuts != null)
        {
            config.Shortcuts.Desktop = ParseShortcutItems(shortcuts.Element("Desktop"));
            config.Shortcuts.StartMenu = ParseShortcutItems(shortcuts.Element("StartMenu"));
        }

        // License
        var license = root.Element("License");
        if (license != null)
        {
            var typeStr = license.Element("Type")?.Value ?? "text";
            config.License.Type = typeStr switch
            {
                "default" => LicenseType.Default,
                "file" => LicenseType.File,
                _ => LicenseType.Text
            };
            config.License.FilePath = ResolvePath(license.Element("FilePath")?.Value, basePath);
            config.License.Text = license.Element("Text")?.Value ?? string.Empty;
        }

        // Uninstall
        var uninstall = root.Element("Uninstall");
        if (uninstall != null)
        {
            var cleanupStr = uninstall.Element("Cleanup")?.Value;
            config.Uninstall.Cleanup = cleanupStr == "All" ? CleanupType.All : CleanupType.PreserveUserData;
        }

        // Output
        var output = root.Element("Output");
        if (output != null)
        {
            config.Output.Folder = ResolvePath(output.Element("Folder")?.Value, basePath);
        }

        // PostInstall
        var postInstall = root.Element("PostInstall");
        if (postInstall != null)
        {
            config.PostInstall.OpenInstallFolder = postInstall.Element("OpenInstallFolder")?.Value?.ToLower() == "true";
            config.PostInstall.RunAfterInstall = postInstall.Element("RunAfterInstall")?.Value?.ToLower() == "true";
            config.PostInstall.RunAfterInstallPath = postInstall.Element("RunAfterInstallPath")?.Value ?? string.Empty;
        }

        return config;
    }

    public void SaveToFile(InstallerConfig config, string filePath)
    {
        var doc = new XDocument(
            new XDeclaration("1.0", "utf-8", null),
            new XElement("EasyNsisConfig",
                new XAttribute("version", ConfigVersion),

                new XElement("BasicInfo",
                    new XElement("CompanyName", config.BasicInfo.CompanyName),
                    new XElement("ApplicationName", config.BasicInfo.ApplicationName),
                    new XElement("Version", config.BasicInfo.Version),
                    new XElement("Language",
                        new XAttribute("type", config.BasicInfo.Language.Type == LanguageType.Fixed ? "fixed" : "locale"),
                        new XAttribute("value", config.BasicInfo.Language.Value)),
                    new XElement("RegistrationMode", config.BasicInfo.RegistrationMode.ToString()),
                    new XElement("InstallerIcon", config.BasicInfo.InstallerIcon)),

                new XElement("SourceFolder",
                    new XElement("Path", config.SourceFolder.Path)),

                new XElement("InstallDestination",
                    new XElement("Type", config.InstallDestination.Type.ToString()),
                    new XElement("CustomPath", config.InstallDestination.CustomPath),
                    new XElement("AllowUserChange", config.InstallDestination.AllowUserChange.ToString().ToLower())),

                new XElement("Shortcuts",
                    new XElement("Desktop", config.Shortcuts.Desktop.Select(CreateShortcutElement)),
                    new XElement("StartMenu", config.Shortcuts.StartMenu.Select(CreateShortcutElement))),

                new XElement("License",
                    new XElement("Type", config.License.Type.ToString().ToLower()),
                    new XElement("FilePath", config.License.FilePath),
                    new XElement("Text", config.License.Text)),

                new XElement("Uninstall",
                    new XElement("Cleanup", config.Uninstall.Cleanup.ToString())),

                new XElement("Output",
                    new XElement("Folder", config.Output.Folder)),

                new XElement("PostInstall",
                    new XElement("OpenInstallFolder", config.PostInstall.OpenInstallFolder.ToString().ToLower()),
                    new XElement("RunAfterInstall", config.PostInstall.RunAfterInstall.ToString().ToLower()),
                    new XElement("RunAfterInstallPath", config.PostInstall.RunAfterInstallPath))
            )
        );

        doc.Save(filePath);
    }

    private static string ResolvePath(string? path, string basePath)
    {
        if (string.IsNullOrWhiteSpace(path))
            return string.Empty;

        if (Path.IsPathRooted(path))
            return path;

        return Path.GetFullPath(Path.Combine(basePath, path));
    }

    private static List<ShortcutItem> ParseShortcutItems(XElement? parent)
    {
        if (parent == null)
            return [];

        return parent.Elements("Item")
            .Select(e => new ShortcutItem
            {
                Type = e.Attribute("type")?.Value == "folder" ? ShortcutItemType.Folder : ShortcutItemType.File,
                Path = e.Value
            })
            .ToList();
    }

    private static XElement CreateShortcutElement(ShortcutItem item)
    {
        return new XElement("Item",
            new XAttribute("type", item.Type == ShortcutItemType.Folder ? "folder" : "file"),
            item.Path);
    }
}
