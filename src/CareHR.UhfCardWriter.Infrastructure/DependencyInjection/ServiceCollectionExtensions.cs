using CareHR.UhfCardWriter.Application.Abstractions;
using CareHR.UhfCardWriter.Application.DependencyInjection;
using CareHR.UhfCardWriter.Infrastructure.Devices;
using CareHR.UhfCardWriter.Infrastructure.Registration;
using CareHR.UhfCardWriter.Sdk;
using Microsoft.Extensions.DependencyInjection;

namespace CareHR.UhfCardWriter.Infrastructure.DependencyInjection;

/// <summary>
/// Registers CareHR card Infrastructure adapters, SDK session, and Application Services composition.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds card device adapters, registry adapter, and a singleton <see cref="IUhfSdk"/> session.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="apiOptions">CareHR registry API options; empty options yield registration Fail (not throw).</param>
    /// <returns>The same service collection.</returns>
    /// <remarks>
    /// Application depends on <c>ICard*</c> ports only. SDK types remain inside Infrastructure.
    /// Lifetime: Singleton session — not thread-safe; callers must serialize access.
    /// Does not register Application Services — use <see cref="AddCareHrCardWriter"/> or
    /// <c>AddApplicationServices</c> from the composition root.
    /// </remarks>
    public static IServiceCollection AddUhfInfrastructure(
        this IServiceCollection services,
        CareHrCardApiOptions? apiOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton(apiOptions ?? new CareHrCardApiOptions());
        services.AddSingleton<IUhfSdk>(_ => new UhfPrimeSdk());
        services.AddSingleton<ICardConnection, CardConnectionAdapter>();
        services.AddSingleton<ICardScanner, CardScannerAdapter>();
        services.AddSingleton<ICardWriter, CardWriterAdapter>();
        services.AddSingleton<ICardReader, CardReaderAdapter>();
        services.AddSingleton<ICardSecurity, CardSecurityAdapter>();
        services.AddSingleton<ICardRegistrar, HttpCardRegistrarAdapter>();

        return services;
    }

    /// <summary>
    /// Full runtime composition: Application Services + Infrastructure ports/adapters + SDK.
    /// </summary>
    /// <param name="services">Service collection.</param>
    /// <param name="configureApi">Optional CareHR registry API options.</param>
    /// <returns>The same service collection.</returns>
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
