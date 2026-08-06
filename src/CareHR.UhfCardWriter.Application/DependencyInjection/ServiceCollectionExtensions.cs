using CareHR.UhfCardWriter.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace CareHR.UhfCardWriter.Application.DependencyInjection;

/// <summary>
/// Registers CareHR Application Services (no Infrastructure / SDK types).
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Adds Application Services and the write-card orchestrator.
    /// </summary>
    /// <remarks>
    /// Lifetime: Singleton — matches Infrastructure single reader session; callers must serialize device access.
    /// Does not register ports; call Infrastructure <c>AddUhfInfrastructure</c> from the composition root.
    /// </remarks>
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<CardConnectionService>();
        services.AddSingleton<CardScanningService>();
        services.AddSingleton<CardReadingService>();
        services.AddSingleton<CardWritingService>();
        services.AddSingleton<CardVerificationService>();
        services.AddSingleton<CardRegistrationService>();
        services.AddSingleton<CardWriteOrchestrator>();

        return services;
    }
}
