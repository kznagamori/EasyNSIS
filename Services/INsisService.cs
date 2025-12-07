using EasyNSIS.Models;

namespace EasyNSIS.Services;

public interface INsisService
{
    bool ValidateNsisInstallation(out string errorMessage);
    string GenerateScript(InstallerConfig config);
    Task<(bool Success, string Output)> BuildInstallerAsync(
        string scriptPath,
        IProgress<string>? progress = null,
        CancellationToken cancellationToken = default);
}
