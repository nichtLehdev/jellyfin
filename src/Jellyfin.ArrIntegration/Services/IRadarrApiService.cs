#pragma warning disable CS1591

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.ArrIntegration.Models;

namespace Jellyfin.ArrIntegration.Services;

/// <summary>
/// Client for Radarr API (movies).
/// </summary>
public interface IRadarrApiService
{
    /// <summary>
    /// Test connection to Radarr with the given base URL and API key.
    /// </summary>
    Task<bool> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Search for movies by term (Radarr movie/lookup).
    /// </summary>
    Task<IReadOnlyList<RadarrMovieResource>> LookupAsync(string term, CancellationToken cancellationToken = default);

    /// <summary>
    /// Add a movie to Radarr (monitor and optionally search for download).
    /// </summary>
    Task<RadarrMovieResource> AddMovieAsync(RadarrMovieResource movie, bool searchForMovie = true, CancellationToken cancellationToken = default);

    /// <summary>
    /// Get the current download queue from Radarr.
    /// </summary>
    Task<IReadOnlyList<ArrQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default);
}
