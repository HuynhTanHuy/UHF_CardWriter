using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.Application.Services;

namespace CareHR.UhfCardWriter.App.Diagnostics;

internal static class HealthChecker
{
    public sealed record Check(string Name, bool Ready, string Detail);

    public static IReadOnlyList<Check> Run(
        AppSettings settings,
        CardConnectionService connection,
        string? readerLabel,
        bool authSessionReady)
    {
        var cfg = ConfigurationValidator.Validate(settings);
        var cfgReady = !ConfigurationValidator.HasBlockingErrors(cfg);
        var cfgDetail = cfgReady
            ? $"OK ({cfg.Count(c => c.Severity == "Warning")} warning(s))"
            : string.Join("; ", cfg.Where(c => c.Severity == "Error").Select(c => c.Message));

        var urlReady = !string.IsNullOrWhiteSpace(settings.Api.BaseUrl)
                       && Uri.TryCreate(settings.Api.BaseUrl.Trim(), UriKind.Absolute, out _);

        return new[]
        {
            new Check("Configuration", cfgReady, cfgDetail),
            new Check("Native DLL (UHFPrimeReader)", DiagnosticsInfo.NativeDllPresent,
                DiagnosticsInfo.NativeDllPresent ? "Found next to EXE" : "UHFPrimeReader.dll missing"),
            new Check("Native DLL (hidapi)", DiagnosticsInfo.HidApiDllPresent,
                DiagnosticsInfo.HidApiDllPresent ? "Found next to EXE" : "hidapi.dll missing"),
            new Check("Backend URL", urlReady, urlReady ? settings.Api.BaseUrl : "Api.BaseUrl invalid"),
            new Check("Auth Session", authSessionReady,
                authSessionReady ? "JWT session active" : "Authentication session required"),
            new Check("Reader", connection.IsConnected,
                connection.IsConnected
                    ? $"Connected: {readerLabel ?? "(unknown)"}"
                    : "Not connected"),
        };
    }
}
