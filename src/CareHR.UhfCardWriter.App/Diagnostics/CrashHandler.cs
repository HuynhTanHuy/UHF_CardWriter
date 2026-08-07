using System.Text;

namespace CareHR.UhfCardWriter.App.Diagnostics;

/// <summary>Registers global crash handlers and writes crash reports under LocalAppData.</summary>
internal static class CrashHandler
{
    private static int _wired;

    public static void Wire()
    {
        if (Interlocked.Exchange(ref _wired, 1) == 1)
            return;

        System.Windows.Forms.Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
        System.Windows.Forms.Application.ThreadException += (_, e) => Handle("UI", e.Exception, isTerminating: false);
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var ex = e.ExceptionObject as Exception
                     ?? new Exception(e.ExceptionObject?.ToString() ?? "Unknown unhandled exception");
            Handle("AppDomain", ex, e.IsTerminating);
        };
        TaskScheduler.UnobservedTaskException += (_, e) =>
        {
            Handle("Task", e.Exception, isTerminating: false);
            e.SetObserved();
        };
    }

    public static string WriteCrashReport(string source, Exception ex, bool isTerminating)
    {
        AppPaths.EnsureCreated();
        var path = Path.Combine(AppPaths.Crashes, $"crash-{DateTime.Now:yyyyMMdd-HHmmss-fff}.txt");
        try
        {
            var sb = new StringBuilder();
            sb.AppendLine("CareHR UHF Card Writer — Crash Report");
            sb.AppendLine($"Timestamp (local): {DateTime.Now:O}");
            sb.AppendLine($"Source: {source}");
            sb.AppendLine($"Terminating: {isTerminating}");
            sb.AppendLine($"Version: {DiagnosticsInfo.ApplicationVersion}");
            sb.AppendLine($"Runtime: {DiagnosticsInfo.DotNetRuntime}");
            sb.AppendLine($"OS: {DiagnosticsInfo.OsDescription}");
            sb.AppendLine();
            sb.AppendLine("--- Exception ---");
            sb.AppendLine(AppLog.Redact(ex.ToString()));
            sb.AppendLine();
            sb.AppendLine("--- Recent log (redacted) ---");
            foreach (var line in AppLog.RecentLines.TakeLast(80))
                sb.AppendLine(line);

            File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
            AppLog.Error("Crash", $"Crash report written: {path}");
            return path;
        }
        catch (Exception writeEx)
        {
            AppLog.Error("Crash", "Failed to write crash report", writeEx);
            return string.Empty;
        }
    }

    private static void Handle(string source, Exception ex, bool isTerminating)
    {
        var path = WriteCrashReport(source, ex, isTerminating);
        AppLog.Error("Crash", $"{source}: {AppLog.SanitizeException(ex)}");

        try
        {
            var msg = "An unexpected error occurred.\n\n"
                      + UserMessage.ForException(ex)
                      + (string.IsNullOrEmpty(path) ? string.Empty : $"\n\nCrash report:\n{path}");
            MessageBox.Show(msg, "CareHR UHF Card Writer", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        catch
        {
            // Ignore UI failures during crash handling.
        }
    }
}
