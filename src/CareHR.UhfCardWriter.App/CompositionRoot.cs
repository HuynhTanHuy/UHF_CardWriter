using CareHR.UhfCardWriter.App.Configuration;
using CareHR.UhfCardWriter.App.Forms;
using CareHR.UhfCardWriter.Infrastructure.DependencyInjection;
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
    /// Loads <c>appsettings.json</c>, registers Application + Infrastructure, and host forms.
    /// </summary>
    public static ServiceProvider CreateServiceProvider(string? basePath = null)
    {
        var path = basePath ?? AppContext.BaseDirectory;
        var configuration = new ConfigurationBuilder()
            .SetBasePath(path)
            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
            .Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.Configure<AppSettings>(configuration);
        services.AddSingleton(sp => sp.GetRequiredService<IOptions<AppSettings>>().Value);

        var settings = configuration.Get<AppSettings>() ?? new AppSettings();
        services.AddCareHrCardWriter(api =>
        {
            api.BaseUrl = settings.Api.BaseUrl;
            api.BearerToken = settings.Api.BearerToken;
            api.CreateRfidTagPath = settings.Api.CreateRfidTagPath;
        });
        services.AddTransient<MainForm>();

        return services.BuildServiceProvider();
    }
}
