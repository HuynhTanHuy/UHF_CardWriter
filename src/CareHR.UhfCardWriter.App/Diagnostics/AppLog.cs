using System.Text;
using System.Text.RegularExpressions;

namespace CareHR.UhfCardWriter.App.Diagnostics;

/// <summary>
/// Simple file + memory logger for Presentation/host.
/// Never writes Bearer tokens, passwords, or native buffers.
/// </summary>
internal static class AppLog
{
    private static readonly object Gate = new();
    private static readonly List<string> Recent = new(capacity: 512);
    private static string _logFile = string.Empty;
    private static DateTime _startedUtc = DateTime.UtcNow;

    private static readonly Regex JwtLike = new(
        @"eyJ[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+\.[A-Za-z0-9_-]+",
        RegexOptions.Compiled);

    private static readonly Regex BearerHeader = new(
        @"Bearer\s+[A-Za-z0-9\-._~+/]+=*",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string LogFilePath => _logFile;
    public static DateTime StartedUtc => _startedUtc;
    public static IReadOnlyList<string> RecentLines
    {
        get
        {
            lock (Gate)
                return Recent.ToArray();
        }
    }

    public static void Initialize()
    {
        AppPaths.EnsureCreated();
        _startedUtc = DateTime.UtcNow;
        _logFile = Path.Combine(AppPaths.Logs, $"app-{DateTime.Now:yyyyMMdd-HHmmss}.log");
        Info("App", $"Log file: {_logFile}");
    }

    public static void Info(string layer, string message) => Write("INFO", layer, message);
    public static void Warn(string layer, string message) => Write("WARN", layer, message);
    public static void Error(string layer, string message) => Write("ERROR", layer, message);

    public static void Error(string layer, string message, Exception ex) =>
        Write("ERROR", layer, message + " | " + SanitizeException(ex));

    public static void Operation(string action, string result, long? durationMs = null)
    {
        var suffix = durationMs is null ? string.Empty : $" ({durationMs} ms)";
        Write("INFO", "Operation", $"{action}: {result}{suffix}");
    }

    public static string Redact(string? text)
    {
        if (string.IsNullOrEmpty(text))
            return string.Empty;

        var s = BearerHeader.Replace(text, "Bearer ***");
        s = JwtLike.Replace(s, "***JWT***");
        s = s.Replace("AccessPasswordHex", "AccessPassword***", StringComparison.OrdinalIgnoreCase);
        return s;
    }

    public static string SanitizeException(Exception ex)
    {
        // Operator-facing: type + message only (no stack in UI log). Full stack goes to crash report only.
        return Redact($"{ex.GetType().Name}: {ex.Message}");
    }

    private static void Write(string level, string layer, string message)
    {
        var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t{level}\t{layer}\t{Redact(message)}";
        lock (Gate)
        {
            Recent.Add(line);
            if (Recent.Count > 500)
                Recent.RemoveRange(0, Recent.Count - 500);

            try
            {
                if (string.IsNullOrEmpty(_logFile))
                    return;
                File.AppendAllText(_logFile, line + Environment.NewLine, Encoding.UTF8);
            }
            catch
            {
                // Logging must never crash the app.
            }
        }
    }
}
