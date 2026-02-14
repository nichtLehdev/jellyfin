#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.ArrIntegration.Models;

namespace Jellyfin.ArrIntegration.Services;

/// <summary>
/// Client for Sonarr API (TV series).
/// </summary>
public interface ISonarrApiService
{
    /// <summary>
    /// Test connection to Sonarr with the given base URL and API key.
    /// </summary>
    Task<bool> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for series by term (Sonarr series/lookup).
    /// </summary>
    Task<IReadOnlyList<SonarrSeriesResource>> LookupAsync(string term, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a series to Sonarr (monitor and optionally search for episodes).
    /// </summary>
    Task<SonarrSeriesResource> AddSeriesAsync(SonarrSeriesResource series, bool searchForMissingEpisodes = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current download queue from Sonarr.
    /// </summary>
    Task<IReadOnlyList<ArrQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default);
}
