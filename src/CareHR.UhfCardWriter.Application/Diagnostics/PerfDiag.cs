using System.Diagnostics;

namespace CareHR.UhfCardWriter.Application.Diagnostics;

/// <summary>
/// Temporary performance audit logger → %LocalAppData%\CareHR\UhfCardWriter\logs\write-diag.log.
/// Does not alter RFID / business semantics.
/// </summary>
internal static class PerfDiag
{
    public static void Log(string message)
    {
        try
        {
            var dir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "CareHR",
                "UhfCardWriter",
                "logs");
            Directory.CreateDirectory(dir);
            var line = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}\t[Perf] {message}";
            File.AppendAllText(Path.Combine(dir, "write-diag.log"), line + Environment.NewLine);
        }
        catch
        {
            // Diagnostic only — never fail the workflow because of logging.
        }
    }

    public static long Time(string operation, Action action)
    {
        Log($"{operation}.Start");
        var sw = Stopwatch.StartNew();
        try
        {
            action();
            sw.Stop();
            Log($"{operation}.End ElapsedMs={sw.ElapsedMilliseconds} Status=OK");
            return sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log($"{operation}.End ElapsedMs={sw.ElapsedMilliseconds} Status=EXCEPTION {ex.GetType().Name}");
            throw;
        }
    }

    public static T Time<T>(string operation, Func<T> func, Func<T, string>? statusSelector = null)
    {
        Log($"{operation}.Start");
        var sw = Stopwatch.StartNew();
        try
        {
            var result = func();
            sw.Stop();
            var status = statusSelector?.Invoke(result) ?? "OK";
            Log($"{operation}.End ElapsedMs={sw.ElapsedMilliseconds} Status={status}");
            return result;
        }
        catch (Exception ex)
        {
            sw.Stop();
            Log($"{operation}.End ElapsedMs={sw.ElapsedMilliseconds} Status=EXCEPTION {ex.GetType().Name}");
            throw;
        }
    }
}
