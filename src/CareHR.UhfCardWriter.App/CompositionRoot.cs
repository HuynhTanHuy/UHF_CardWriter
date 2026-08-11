using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Diagnostics;
using CareHR.UhfCardWriter.App.Forms;
using CareHR.UhfCardWriter.Infrastructure.DependencyInjection;
using CareHR.UhfCardWriter.Infrastructure.Devices;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace CareHR.UhfCardWriter.App;

/// <summary>
/// Composition root for CareHR UHF Card Writer (wires config + DI; no UI logic).
/// </summary>
public static class CompositionRoot
{
    /// <summary>
    /// Loads <c>appsettings.json</c> (+ optional <c>appsettings.Local.json</c>), registers Application + Infrastructure.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(string? basePath = null)
    {
        var path = basePath ?? AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .AddJsonFile("appsettings.Local.json", optional: true, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        var settings = configuration.Get<AppSettings>() ?? new AppSettings();
        var findings = ConfigurationValidator.Validate(settings);
        foreach (var f in findings)
        {
            if (string.Equals(f.Severity, "Error", StringComparison.OrdinalIgnoreCase))
                AppLog.Error("Config", $"{f.Code}: {f.Message}");
            else
                AppLog.Warn("Config", $"{f.Code}: {f.Message}");
        }

        if (!DiagnosticsInfo.NativeDllPresent)
            AppLog.Error("Startup", "UHFPrimeReader.dll missing next to EXE.");
        if (!DiagnosticsInfo.HidApiDllPresent)
            AppLog.Warn("Startup", "hidapi.dll missing next to EXE.");

        var uhf = DiagnosticsInfo.DescribeNativeDll("UHFPrimeReader.dll");
        var hid = DiagnosticsInfo.DescribeNativeDll("hidapi.dll");
        AppLog.Info(
            "Startup",
            $"ProcessArch={DiagnosticsInfo.ProcessArchitecture} " +
            $"UhfPrimeReaderArch={uhf.Arch} UhfPrimeReaderVersion={uhf.Version} UhfPrimeReaderSize={uhf.Size} SHA256={uhf.Sha256} " +
            $"hidapiArch={hid.Arch} hidapiSize={hid.Size}");
        AppLog.Info("Startup", $"Loaded UHFPrimeReader: {uhf.Path}");
        AppLog.Info("Startup", $"Loaded hidapi: {hid.Path}");
        AppLog.Info("Startup", $"Architecture: {DiagnosticsInfo.ProcessArchitecture}");

        services.AddCareHrCardWriter(api =>
        {
            api.BaseUrl = settings.Api.BaseUrl;
            api.BearerToken = settings.Api.BearerToken;
            api.CreateRfidCardPath = settings.Api.CreateRfidCardPath;
            api.DefaultStatus = settings.Api.DefaultStatus;
            api.DefaultIsActive = settings.Api.DefaultIsActive;
            api.DefaultHospitalId = settings.Hospitals.Count > 0
                ? settings.Hospitals[0].Id
                : string.Empty;
        });
        CardConnectionAdapter.DeviceParaDiag = msg => AppLog.Info("DevicePara", msg);
        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
    }
}
