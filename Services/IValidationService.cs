using EasyNSIS.Models;

namespace EasyNSIS.Services;

public interface IValidationService
{
    ValidationResult ValidateAll(InstallerConfig config);
    ValidationResult ValidateBasicInfo(BasicInfo basicInfo);
    ValidationResult ValidateSourceFolder(SourceFolder sourceFolder);
    ValidationResult ValidateInstallDestination(InstallDestination destination);
    ValidationResult ValidateShortcuts(ShortcutSettings shortcuts, string sourceFolderPath);
    ValidationResult ValidateLicense(LicenseSettings license);
    ValidationResult ValidateOutput(OutputSettings output);
    ValidationResult ValidateVersion(string version);
    ValidationResult ValidatePath(string path, string fieldName, bool mustExist = false);
    ValidationResult ValidateCompanyOrAppName(string name, string fieldName);
}
