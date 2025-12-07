namespace EasyNSIS.Services;

public interface ILogService
{
    void LogError(string message, Exception? exception = null);
    void LogBuild(string message);
}
