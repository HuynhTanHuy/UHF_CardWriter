using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.DependencyInjection;
using CareHR.UhfCardWriter.Infrastructure.Auth;
using CareHR.UhfCardWriter.Infrastructure.Devices;
using CareHR.UhfCardWriter.Infrastructure.Registration;
using CareHR.UhfCardWriter.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace CareHR.UhfCardWriter.Infrastructure.DependencyInjection;

/// <summary>
/// Registers CareHR card Infrastructure adapters and a singleton SDK session.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Wires <c>ICard*</c> adapters to a singleton <see cref="IUhfSdk"/> (not thread-safe — serialize access).
    /// </summary>
    /// <remarks>Does not register Application Services — use <see cref="AddCareHrCardWriter"/> or <c>AddApplicationServices</c>.</remarks>
    public static IServiceCollection AddUhfInfrastructure(
        this IServiceCollection services,
        CareHrCardApiOptions? apiOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(apiOptions ?? new CareHrCardApiOptions());
        services.AddSingleton<IWriterAuthSession, InMemoryWriterAuthSession>();
        services.AddSingleton<IUhfSdk>(_ => new UhfPrimeSdk());
        services.AddSingleton<ICardConnection, CardConnectionAdapter>();
        services.AddSingleton<ICardScanner, CardScannerAdapter>();
        services.AddSingleton<ICardWriter, CardWriterAdapter>();
        services.AddSingleton<ICardReader, CardReaderAdapter>();
        services.AddSingleton<ICardSecurity, CardSecurityAdapter>();
        services.AddSingleton<ICardRegistrar, HttpCardRegistrarAdapter>();

        return services;
    }

    /// <summary>Application Services + Infrastructure adapters + SDK session.</summary>
    public static IServiceCollection AddCareHrCardWriter(
        this IServiceCollection services,
        Action<CareHrCardApiOptions>? configureApi = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        var options = new CareHrCardApiOptions();
        configureApi?.Invoke(options);

        services.AddApplicationServices();
        services.AddUhfInfrastructure(options);

        return services;
    }
}
