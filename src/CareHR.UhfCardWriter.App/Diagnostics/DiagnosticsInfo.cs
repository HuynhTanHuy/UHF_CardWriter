using System.Reflection;
using System.Runtime.InteropServices;
using CareHR.UhfCardWriter.App.Configuration;

namespace CareHR.UhfCardWriter.App.Diagnostics;

/// <summary>Static environment / version facts for About and diagnostics export.</summary>
internal static class DiagnosticsInfo
{
    public static string ApplicationName => "CareHR UHF Card Writer";

    public static string ApplicationVersion
    {
        get
        {
            var asm = Assembly.GetExecutingAssembly();
            return asm.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
                   ?? asm.GetName().Version?.ToString()
                   ?? "unknown";
        }
    }

    public static string BuildDate
    {
        get
        {
            try
            {
                var path = Assembly.GetExecutingAssembly().Location;
                if (string.IsNullOrEmpty(path) || !File.Exists(path))
                    return "unknown";
                return File.GetLastWriteTime(path).ToString("yyyy-MM-dd HH:mm");
            }
            catch
            {
                return "unknown";
            }
        }
    }

    public static string DotNetRuntime => RuntimeInformation.FrameworkDescription;
    public static string OsDescription => RuntimeInformation.OSDescription;
    public static string ProcessArchitecture => RuntimeInformation.ProcessArchitecture.ToString();
    public static string BaseDirectory => AppContext.BaseDirectory;

    public static bool NativeDllPresent =>
        File.Exists(Path.Combine(BaseDirectory, "UHFPrimeReader.dll"));

    public static bool HidApiDllPresent =>
        File.Exists(Path.Combine(BaseDirectory, "hidapi.dll"));

    public static string Summarize(AppSettings settings, bool readerConnected, string? readerLabel)
    {
        return string.Join(Environment.NewLine, new[]
        {
            $"{ApplicationName}",
            $"Version: {ApplicationVersion}",
            $"Build file time: {BuildDate}",
            $".NET: {DotNetRuntime}",
            $"OS: {OsDescription}",
            $"Arch: {ProcessArchitecture}",
            $"BaseDir: {BaseDirectory}",
            $"Log folder: {AppPaths.Logs}",
            $"API URL: {settings.Api.BaseUrl}",
            $"API path: {settings.Api.CreateRfidCardPath}",
            $"Bearer token: {(string.IsNullOrWhiteSpace(settings.Api.BearerToken) ? "(empty)" : "(set)")}",
            $"Hospitals: {settings.Hospitals.Count}",
            $"Card types: {settings.CardTypes.Count}",
            $"Native UHFPrimeReader.dll: {(NativeDllPresent ? "present" : "MISSING")}",
            $"Native hidapi.dll: {(HidApiDllPresent ? "present" : "MISSING")}",
            $"Reader connected: {readerConnected}",
            $"Current reader: {readerLabel ?? "(none)"}",
        });
    }
}
