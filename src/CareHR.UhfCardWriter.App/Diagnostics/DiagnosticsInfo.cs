using System.Reflection;
using System.Runtime.InteropServices;
using CareHR.UhfCardWriter.App.Configuration;

namespace CareHR.UhfCardWriter.App.Diagnostics;

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

    public readonly record struct NativeDllInfo(string Path, string Arch, string Version, long Size, string Sha256);

    public static NativeDllInfo DescribeNativeDll(string fileName)
    {
        var path = Path.Combine(BaseDirectory, fileName);
        if (!File.Exists(path))
            return new NativeDllInfo(path, "missing", "missing", 0, "missing");

        try
        {
            var fi = new FileInfo(path);
            var ver = System.Diagnostics.FileVersionInfo.GetVersionInfo(path).FileVersion ?? string.Empty;
            var sha = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(path)));
            var arch = ReadPeArchitecture(path);
            return new NativeDllInfo(path, arch, ver, fi.Length, sha);
        }
        catch (Exception ex)
        {
            return new NativeDllInfo(path, "error", ex.Message, 0, "error");
        }
    }

    private static string ReadPeArchitecture(string path)
    {
        using var fs = File.OpenRead(path);
        using var br = new BinaryReader(fs);
        if (br.ReadUInt16() != 0x5A4D)
            return "unknown";
        fs.Seek(0x3C, SeekOrigin.Begin);
        var peOffset = br.ReadInt32();
        fs.Seek(peOffset, SeekOrigin.Begin);
        if (br.ReadUInt32() != 0x00004550)
            return "unknown";
        var machine = br.ReadUInt16();
        return machine switch
        {
            0x014c => "x86",
            0x8664 => "x64",
            0xAA64 => "ARM64",
            _ => $"0x{machine:X4}",
        };
    }

    public static string Summarize(AppSettings settings, bool readerConnected, string? readerLabel, bool authSessionReady)
    {
        var uhf = DescribeNativeDll("UHFPrimeReader.dll");
        var hid = DescribeNativeDll("hidapi.dll");
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
            $"Auth session: {(authSessionReady ? "active" : "required")}",
            $"Hospitals: {settings.Hospitals.Count}",
            $"Card types: {settings.CardTypes.Count}",
            $"Native UHFPrimeReader.dll: {(NativeDllPresent ? $"present Arch={uhf.Arch} Ver={uhf.Version} Size={uhf.Size}" : "MISSING")}",
            $"Native hidapi.dll: {(HidApiDllPresent ? $"present Arch={hid.Arch} Size={hid.Size}" : "MISSING")}",
            $"Reader connected: {readerConnected}",
            $"Current reader: {readerLabel ?? "(none)"}",
        });
    }
}
