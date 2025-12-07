using EasyNSIS.Models;

namespace EasyNSIS.Services;

public interface IConfigurationService
{
    InstallerConfig LoadFromFile(string filePath);
    void SaveToFile(InstallerConfig config, string filePath);
    InstallerConfig CreateDefault();
}
