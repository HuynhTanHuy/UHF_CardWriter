using System.Diagnostics;
using System.Text;
using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.Application.Services;

namespace CareHR.UhfCardWriter.App.Diagnostics;

internal static class DiagnosticsExporter
{
    public static string Export(
        AppSettings settings,
        CardConnectionService connection,
        string? readerLabel,
        bool authSessionReady,
        IEnumerable<string>? operationLines,
        IReadOnlyDictionary<string, string>? timings)
    {
        AppPaths.EnsureCreated();
        var path = Path.Combine(AppPaths.Exports, $"diagnostics-{DateTime.Now:yyyyMMdd-HHmmss}.txt");
        var sb = new StringBuilder();

        sb.AppendLine("CareHR UHF Card Writer — Diagnostics Export");
        sb.AppendLine($"Exported (local): {DateTime.Now:O}");
        sb.AppendLine();
        sb.AppendLine("=== Application ===");
        sb.AppendLine(DiagnosticsInfo.Summarize(settings, connection.IsConnected, readerLabel, authSessionReady));
        sb.AppendLine();

        sb.AppendLine("=== Health ===");
        foreach (var c in HealthChecker.Run(settings, connection, readerLabel, authSessionReady))
            sb.AppendLine($"{(c.Ready ? "OK" : "FAIL")}  {c.Name}: {c.Detail}");
        sb.AppendLine();

        sb.AppendLine("=== Configuration findings ===");
        foreach (var f in ConfigurationValidator.Validate(settings))
            sb.AppendLine($"[{f.Severity}] {f.Code}: {f.Message}");
        sb.AppendLine();

        sb.AppendLine("=== Configuration summary (secrets redacted) ===");
        sb.AppendLine($"BaseUrl={settings.Api.BaseUrl}");
        sb.AppendLine($"CreateRfidCardPath={settings.Api.CreateRfidCardPath}");
        sb.AppendLine($"AuthSession={(authSessionReady ? "active" : "required")}");
        sb.AppendLine($"DefaultStatus={settings.Api.DefaultStatus}");
        sb.AppendLine($"DefaultIsActive={settings.Api.DefaultIsActive}");
        sb.AppendLine($"Reader.DefaultMode={settings.Reader.DefaultMode}");
        sb.AppendLine($"Reader.ComPort={settings.Reader.ComPort}");
        sb.AppendLine($"Reader.BaudRate={settings.Reader.BaudRate}");
        sb.AppendLine($"Reader.ScanTimeoutMs={settings.Reader.ScanTimeoutMs}");
        sb.AppendLine($"Card.DefaultBatchNumber={settings.Card.DefaultBatchNumber}");
        sb.AppendLine($"Card.BatchNumberWidth={settings.Card.BatchNumberWidth}");
        sb.AppendLine($"Card.SerialNumberWidth={settings.Card.SerialNumberWidth}");
        foreach (var h in settings.Hospitals)
            sb.AppendLine($"Hospital={h.Name}; Number={h.EffectiveHospitalNumber}; Id={h.Id}");
        sb.AppendLine($"Card.AccessPasswordHex={(string.IsNullOrWhiteSpace(settings.Card.AccessPasswordHex) ? "(empty)" : "(set)")}");
        foreach (var h in settings.Hospitals)
            sb.AppendLine($"Hospital: {h.Name} | Id={h.Id} | Code={h.Code}");
        foreach (var t in settings.CardTypes)
            sb.AppendLine($"CardType: {t.Name} | Id={t.Id}");
        sb.AppendLine();

        sb.AppendLine("=== Performance (this session) ===");
        if (timings is null || timings.Count == 0)
            sb.AppendLine("(no timings recorded yet)");
        else
        {
            foreach (var kv in timings)
                sb.AppendLine($"{kv.Key}: {kv.Value}");
        }

        var proc = Process.GetCurrentProcess();
        sb.AppendLine($"WorkingSetMB: {proc.WorkingSet64 / (1024.0 * 1024.0):F1}");
        sb.AppendLine($"PrivateMemoryMB: {proc.PrivateMemorySize64 / (1024.0 * 1024.0):F1}");
        sb.AppendLine($"Session uptime: {DateTime.UtcNow - AppLog.StartedUtc}");
        sb.AppendLine();

        sb.AppendLine("=== Operation history (UI) ===");
        if (operationLines is null)
            sb.AppendLine("(none)");
        else
        {
            foreach (var line in operationLines)
                sb.AppendLine(AppLog.Redact(line));
        }

        sb.AppendLine();
        sb.AppendLine("=== Recent application log ===");
        foreach (var line in AppLog.RecentLines.TakeLast(200))
            sb.AppendLine(line);

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        AppLog.Info("Support", $"Diagnostics exported: {path}");
        return path;
    }
}
