using System.IO;
using System.Text;

namespace EasyNSIS.Services;

public class LogService : ILogService
{
    private const int MaxLines = 1000;
    private readonly string _errorLogPath;
    private readonly string _buildLogPath;
    private readonly object _lock = new();

    public LogService()
    {
        var basePath = AppContext.BaseDirectory;
        _errorLogPath = Path.Combine(basePath, "Error.log");
        _buildLogPath = Path.Combine(basePath, "NSIS_Build.log");
    }

    public void LogError(string message, Exception? exception = null)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var logEntry = exception != null
            ? $"{timestamp} {message}\n{exception}"
            : $"{timestamp} {message}";

        WriteToLog(_errorLogPath, logEntry);
    }

    public void LogBuild(string message)
    {
        var timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ssZ");
        var logEntry = $"{timestamp} {message}";

        WriteToLog(_buildLogPath, logEntry);
    }

    private void WriteToLog(string logPath, string entry)
    {
        lock (_lock)
        {
            try
            {
                var lines = new List<string>();

                if (File.Exists(logPath))
                {
                    lines.AddRange(File.ReadAllLines(logPath, Encoding.UTF8));
                }

                lines.Add(entry);

                // ローテーション
                if (lines.Count >= MaxLines)
                {
                    var backupPath = logPath + ".1";
                    if (File.Exists(backupPath))
                    {
                        File.Delete(backupPath);
                    }
                    File.Move(logPath, backupPath);
                    lines.Clear();
                    lines.Add(entry);
                }

                File.WriteAllLines(logPath, lines, Encoding.UTF8);
            }
            catch
            {
                // ログ書き込み失敗は無視
            }
        }
    }
}
