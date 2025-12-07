using System.IO;
using System.Text.RegularExpressions;
using EasyNSIS.Models;

namespace EasyNSIS.Services;

public partial class ValidationService : IValidationService
{
    private static readonly char[] InvalidNameChars = ['<', '>', ':', '"', '/', '\\', '|', '?', '*'];

    [GeneratedRegex(@"^\d+(\.\d+){0,3}$")]
    private static partial Regex VersionRegex();

    // 予約ファイル名（ソースフォルダーに含めることができない）
    private static readonly string[] ReservedFileNames =
    [
        "installed_files.dat",
        "installed_version.dat",
        "Uninstall.exe"
    ];

    public ValidationResult ValidateAll(InstallerConfig config)
    {
        var result = new ValidationResult();
        result.Merge(ValidateBasicInfo(config.BasicInfo));
        result.Merge(ValidateSourceFolder(config.SourceFolder));
        result.Merge(ValidateInstallDestination(config.InstallDestination));
        result.Merge(ValidateShortcuts(config.Shortcuts, config.SourceFolder.Path));
        result.Merge(ValidateLicense(config.License));
        result.Merge(ValidateOutput(config.Output));
        result.Merge(ValidatePostInstall(config.PostInstall, config.SourceFolder.Path));
        return result;
    }

    public ValidationResult ValidateBasicInfo(BasicInfo basicInfo)
    {
        var result = new ValidationResult();

        // 会社名（オプション - 入力がある場合のみバリデーション）
        if (!string.IsNullOrWhiteSpace(basicInfo.CompanyName))
        {
            result.Merge(ValidateCompanyOrAppName(basicInfo.CompanyName, "CompanyName"));
        }

        // アプリケーション名
        if (string.IsNullOrWhiteSpace(basicInfo.ApplicationName))
        {
            result.AddError("ApplicationName", Resources.Strings.Err_RequiredField);
        }
        else
        {
            result.Merge(ValidateCompanyOrAppName(basicInfo.ApplicationName, "ApplicationName"));
        }

        // バージョン
        if (string.IsNullOrWhiteSpace(basicInfo.Version))
        {
            result.AddError("Version", Resources.Strings.Err_RequiredField);
        }
        else
        {
            result.Merge(ValidateVersion(basicInfo.Version));
        }

        // アイコン（指定がある場合のみ）
        if (!string.IsNullOrWhiteSpace(basicInfo.InstallerIcon))
        {
            if (!basicInfo.InstallerIcon.EndsWith(".ico", StringComparison.OrdinalIgnoreCase))
            {
                result.AddError("InstallerIcon", Resources.Strings.Err_InvalidIconFormat);
            }
            else if (!File.Exists(basicInfo.InstallerIcon))
            {
                result.AddError("InstallerIcon", Resources.Strings.Err_FileNotFound);
            }
        }

        return result;
    }

    public ValidationResult ValidateSourceFolder(SourceFolder sourceFolder)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(sourceFolder.Path))
        {
            result.AddError("SourceFolder", Resources.Strings.Err_RequiredField);
        }
        else if (!Directory.Exists(sourceFolder.Path))
        {
            result.AddError("SourceFolder", Resources.Strings.Err_FolderNotFound);
        }
        else
        {
            // 予約ファイル名のチェック
            foreach (var reservedName in ReservedFileNames)
            {
                var reservedPath = Path.Combine(sourceFolder.Path, reservedName);
                if (File.Exists(reservedPath))
                {
                    result.AddError("SourceFolder", string.Format(Resources.Strings.Err_ReservedFileName, reservedName));
                }
            }
        }

        return result;
    }

    public ValidationResult ValidateInstallDestination(InstallDestination destination)
    {
        var result = new ValidationResult();

        if (destination.Type == InstallDestinationType.Custom)
        {
            if (string.IsNullOrWhiteSpace(destination.CustomPath))
            {
                result.AddError("CustomPath", Resources.Strings.Err_RequiredField);
            }
            else
            {
                result.Merge(ValidateCustomInstallPath(destination.CustomPath));
            }
        }

        return result;
    }

    public ValidationResult ValidateCustomInstallPath(string path)
    {
        var result = new ValidationResult();

        var trimmed = path.Trim();

        // 末尾のドット/スペース
        if (trimmed.EndsWith('.') || trimmed.EndsWith(' '))
        {
            result.AddError("CustomPath", Resources.Strings.Err_InvalidPathEnding);
            return result;
        }

        // UNCパスチェック
        if (trimmed.StartsWith(@"\\"))
        {
            result.AddError("CustomPath", Resources.Strings.Err_UncPathNotAllowed);
            return result;
        }

        // NSIS変数を含むパスの処理（$APPDATA等）
        if (trimmed.StartsWith("$"))
        {
            // NSIS変数を使用している場合、基本的な構文チェックのみ
            // 禁止文字チェック（$と\は除く）
            var invalidChars = new[] { '<', '>', ':', '"', '/', '|', '?', '*' };
            if (trimmed.Any(c => invalidChars.Contains(c)))
            {
                result.AddError("CustomPath", Resources.Strings.Err_InvalidPath);
                return result;
            }

            // パストラバーサルチェック
            if (trimmed.Contains(".."))
            {
                result.AddError("CustomPath", Resources.Strings.Err_InvalidPath);
                return result;
            }

            return result;
        }

        // 通常のWindowsパス
        // 相対パス・..チェック
        if (!Path.IsPathRooted(trimmed) || trimmed.Contains(".."))
        {
            result.AddError("CustomPath", Resources.Strings.Err_InvalidPath);
            return result;
        }

        // ドライブルートへのインストール禁止（危険）
        if (Path.GetPathRoot(trimmed) == trimmed)
        {
            result.AddError("CustomPath", Resources.Strings.Err_InvalidPath);
            return result;
        }

        // 禁止文字チェック
        try
        {
            var _ = Path.GetFullPath(trimmed);
        }
        catch
        {
            result.AddError("CustomPath", Resources.Strings.Err_InvalidPath);
        }

        return result;
    }

    public ValidationResult ValidateShortcuts(ShortcutSettings shortcuts, string sourceFolderPath)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(sourceFolderPath))
        {
            return result;
        }

        foreach (var item in shortcuts.Desktop)
        {
            var fullPath = Path.Combine(sourceFolderPath, item.Path);
            if (item.Type == ShortcutItemType.File && !File.Exists(fullPath))
            {
                result.AddError("DesktopShortcut", string.Format(Resources.Strings.Err_ShortcutTargetNotFound, item.Path));
            }
            else if (item.Type == ShortcutItemType.Folder && !Directory.Exists(fullPath))
            {
                result.AddError("DesktopShortcut", string.Format(Resources.Strings.Err_ShortcutTargetNotFound, item.Path));
            }
        }

        foreach (var item in shortcuts.StartMenu)
        {
            var fullPath = Path.Combine(sourceFolderPath, item.Path);
            if (item.Type == ShortcutItemType.File && !File.Exists(fullPath))
            {
                result.AddError("StartMenuShortcut", string.Format(Resources.Strings.Err_ShortcutTargetNotFound, item.Path));
            }
            else if (item.Type == ShortcutItemType.Folder && !Directory.Exists(fullPath))
            {
                result.AddError("StartMenuShortcut", string.Format(Resources.Strings.Err_ShortcutTargetNotFound, item.Path));
            }
        }

        return result;
    }

    public ValidationResult ValidateLicense(LicenseSettings license)
    {
        var result = new ValidationResult();

        if (license.Type == LicenseType.File)
        {
            if (string.IsNullOrWhiteSpace(license.FilePath))
            {
                result.AddError("LicenseFile", Resources.Strings.Err_RequiredField);
            }
            else if (!File.Exists(license.FilePath))
            {
                result.AddError("LicenseFile", Resources.Strings.Err_FileNotFound);
            }
        }
        else if (license.Type == LicenseType.Text)
        {
            if (string.IsNullOrWhiteSpace(license.Text))
            {
                result.AddError("LicenseText", Resources.Strings.Err_RequiredField);
            }
        }

        return result;
    }

    public ValidationResult ValidateOutput(OutputSettings output)
    {
        var result = new ValidationResult();

        if (string.IsNullOrWhiteSpace(output.Folder))
        {
            result.AddError("OutputFolder", Resources.Strings.Err_RequiredField);
        }
        else
        {
            result.Merge(ValidatePath(output.Folder, "OutputFolder", mustExist: false));
        }

        return result;
    }

    public ValidationResult ValidateVersion(string version)
    {
        var result = new ValidationResult();

        if (!VersionRegex().IsMatch(version))
        {
            result.AddError("Version", Resources.Strings.Err_InvalidVersion);
        }

        return result;
    }

    public ValidationResult ValidatePath(string path, string fieldName, bool mustExist = false)
    {
        var result = new ValidationResult();

        var trimmed = path.Trim();

        // 末尾のドット/スペース
        if (trimmed.EndsWith('.') || trimmed.EndsWith(' '))
        {
            result.AddError(fieldName, Resources.Strings.Err_InvalidPathEnding);
            return result;
        }

        // UNCパスチェック
        if (trimmed.StartsWith(@"\\"))
        {
            result.AddError(fieldName, Resources.Strings.Err_UncPathNotAllowed);
            return result;
        }

        // 環境変数展開
        var expanded = Environment.ExpandEnvironmentVariables(trimmed);

        // 相対パス・..チェック
        if (!Path.IsPathRooted(expanded) || expanded.Contains(".."))
        {
            result.AddError(fieldName, Resources.Strings.Err_InvalidPath);
            return result;
        }

        // 存在チェック
        if (mustExist && !Directory.Exists(expanded) && !File.Exists(expanded))
        {
            result.AddError(fieldName, Resources.Strings.Err_PathNotFound);
        }

        return result;
    }

    public ValidationResult ValidatePostInstall(PostInstallSettings postInstall, string sourceFolderPath)
    {
        var result = new ValidationResult();

        // ファイル実行が有効な場合のみチェック
        if (!postInstall.RunAfterInstall)
        {
            return result;
        }

        if (string.IsNullOrWhiteSpace(postInstall.RunAfterInstallPath))
        {
            result.AddError("RunAfterInstallPath", Resources.Strings.Err_RequiredField);
            return result;
        }

        var path = postInstall.RunAfterInstallPath.Trim();

        // パストラバーサルチェック
        if (path.Contains(".."))
        {
            result.AddError("RunAfterInstallPath", Resources.Strings.Err_InvalidPath);
            return result;
        }

        // 絶対パス禁止（相対パスのみ許可）
        if (Path.IsPathRooted(path))
        {
            result.AddError("RunAfterInstallPath", Resources.Strings.Err_InvalidPath);
            return result;
        }

        // ソースフォルダーが指定されている場合、ファイル存在チェック
        if (!string.IsNullOrWhiteSpace(sourceFolderPath) && Directory.Exists(sourceFolderPath))
        {
            var fullPath = Path.Combine(sourceFolderPath, path);
            if (!File.Exists(fullPath))
            {
                result.AddError("RunAfterInstallPath", Resources.Strings.Err_FileNotFound);
                return result;
            }

            // 拡張子チェック（実行可能ファイルのみ）
            var ext = Path.GetExtension(path).ToLowerInvariant();
            var allowedExtensions = new[] { ".exe", ".bat", ".cmd" };
            if (!allowedExtensions.Contains(ext))
            {
                result.AddError("RunAfterInstallPath", Resources.Strings.Err_InvalidExecutableFormat);
            }
        }

        return result;
    }

    public ValidationResult ValidateCompanyOrAppName(string name, string fieldName)
    {
        var result = new ValidationResult();

        // 先頭末尾のスペースチェック（Trim前に実施）
        if (name.StartsWith(' ') || name.EndsWith(' '))
        {
            result.AddError(fieldName, Resources.Strings.Err_InvalidNameEnding);
            return result;
        }

        var trimmed = name.Trim();

        // 長さチェック
        if (trimmed.Length > 64)
        {
            result.AddError(fieldName, Resources.Strings.Err_NameTooLong);
            return result;
        }

        // 禁止文字チェック
        if (trimmed.Any(c => InvalidNameChars.Contains(c)))
        {
            result.AddError(fieldName, Resources.Strings.Err_InvalidPath);
            return result;
        }

        // 先頭末尾のドット
        if (trimmed.StartsWith('.') || trimmed.EndsWith('.'))
        {
            result.AddError(fieldName, Resources.Strings.Err_InvalidNameEnding);
        }

        return result;
    }
}
