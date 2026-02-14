#pragma warning disable CS1591

using Jellyfin.ArrIntegration.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.ArrIntegration.Extensions;

/// <summary>
/// Service collection extensions for Arr integration.
/// </summary>
public static class ArrIntegrationServiceCollectionExtensions
{
    /// <summary>
    /// Adds Radarr and Sonarr API services to the <see cref="IServiceCollection"/>.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <returns>The <see cref="IServiceCollection"/>.</returns>
    public static IServiceCollection AddArrIntegrationServices(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddSingleton<IRadarrApiService, RadarrApiService>();
        services.AddSingleton<ISonarrApiService, SonarrApiService>();
        return services;
    }
}
