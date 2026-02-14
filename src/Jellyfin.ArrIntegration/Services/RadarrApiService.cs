#pragma warning disable CS1591

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.ArrIntegration.Configuration;
using Jellyfin.ArrIntegration.Models;
using MediaBrowser.Common.Configuration;
using Microsoft.Extensions.Logging;

namespace Jellyfin.ArrIntegration.Services;

/// <summary>
/// HTTP client for Radarr API.
/// </summary>
public class RadarrApiService : IRadarrApiService
{
    private readonly IConfigurationManager _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<RadarrApiService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="RadarrApiService"/> class.
    /// </summary>
    public RadarrApiService(
        IConfigurationManager config,
        IHttpClientFactory httpClientFactory,
        ILogger<RadarrApiService> logger)
    {
        _config = config;
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    private ArrIntegrationOptions GetOptions()
    {
        return _config.GetConfiguration<ArrIntegrationOptions>("arrintegration")
            ?? new ArrIntegrationOptions();
    }

    /// <inheritdoc />
    public async Task<bool> TestConnectionAsync(string baseUrl, string apiKey, CancellationToken cancellationToken = default)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/system/status", apiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            return response.IsSuccessStatusCode;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radarr connection test failed for {BaseUrl}", baseUrl);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<RadarrMovieResource>> LookupAsync(string term, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.RadarrEnabled || string.IsNullOrWhiteSpace(options.RadarrBaseUrl) || string.IsNullOrWhiteSpace(options.RadarrApiKey))
        {
            return Array.Empty<RadarrMovieResource>();
        }

        var baseUrl = options.RadarrBaseUrl.TrimEnd('/');
        var path = "api/v3/movie/lookup?term=" + Uri.EscapeDataString(term);
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, path, options.RadarrApiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<RadarrMovieResource>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<RadarrMovieResource>)(list ?? new List<RadarrMovieResource>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radarr lookup failed for term {Term}", term);
            return Array.Empty<RadarrMovieResource>();
        }
    }

    /// <inheritdoc />
    public async Task<RadarrMovieResource> AddMovieAsync(RadarrMovieResource movie, bool searchForMovie = true, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.RadarrEnabled || string.IsNullOrWhiteSpace(options.RadarrBaseUrl) || string.IsNullOrWhiteSpace(options.RadarrApiKey))
        {
            throw new InvalidOperationException("Radarr is not configured or enabled.");
        }

        movie.AddOptions ??= new RadarrAddOptions { SearchForMovie = searchForMovie };
        movie.Monitored = true;
        var baseUrl = options.RadarrBaseUrl.TrimEnd('/');
        var apiKey = options.RadarrApiKey;

        if (movie.QualityProfileId <= 0)
        {
            var profiles = await GetQualityProfilesAsync(baseUrl, apiKey, cancellationToken).ConfigureAwait(false);
            movie.QualityProfileId = profiles.Count > 0 ? profiles[0].Id : 1;
        }

        if (string.IsNullOrWhiteSpace(movie.RootFolderPath))
        {
            var folders = await GetRootFoldersAsync(baseUrl, apiKey, cancellationToken).ConfigureAwait(false);
            if (folders.Count > 0)
            {
                movie.RootFolderPath = folders[0].Path;
            }
        }
        using var request = CreateRequest(HttpMethod.Post, baseUrl, "api/v3/movie", apiKey);
        request.Content = JsonContent.Create(movie, options: JsonOptions);
        using var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var added = await response.Content.ReadFromJsonAsync<RadarrMovieResource>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (added is null)
        {
            throw new InvalidOperationException("Radarr returned no movie data.");
        }

        return added;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArrQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.RadarrEnabled || string.IsNullOrWhiteSpace(options.RadarrBaseUrl) || string.IsNullOrWhiteSpace(options.RadarrApiKey))
        {
            return Array.Empty<ArrQueueItem>();
        }

        var baseUrl = options.RadarrBaseUrl.TrimEnd('/');
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/queue?pageSize=100&includeMovie=true", options.RadarrApiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var queue = await response.Content.ReadFromJsonAsync<RadarrQueueResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (queue?.Records is null || queue.Records.Length == 0)
            {
                return Array.Empty<ArrQueueItem>();
            }

            var list = new List<ArrQueueItem>(queue.Records.Length);
            foreach (var r in queue.Records)
            {
                list.Add(new ArrQueueItem
                {
                    Source = "Radarr",
                    Id = r.Id,
                    Title = r.Movie?.Title ?? r.Title ?? "Unknown",
                    Status = r.Status ?? string.Empty,
                    Progress = r.Progress,
                    Size = r.Size,
                    SizeLeft = r.SizeLeft,
                    TimeLeft = r.TimeLeft ?? string.Empty,
                    EstimatedCompletionTime = r.EstimatedCompletionTime ?? string.Empty,
                    DownloadId = r.DownloadId ?? string.Empty,
                    Movie = r.Movie is null ? null : new ArrQueueMovieInfo { Title = r.Movie.Title, Year = r.Movie.Year }
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Radarr get queue failed.");
            return Array.Empty<ArrQueueItem>();
        }
    }

    private async Task<List<RadarrRootFolder>> GetRootFoldersAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/rootfolder", apiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<RadarrRootFolder>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return list ?? new List<RadarrRootFolder>();
        }
        catch
        {
            return new List<RadarrRootFolder>();
        }
    }

    private async Task<List<RadarrQualityProfile>> GetQualityProfilesAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/qualityprofile", apiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<RadarrQualityProfile>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return list ?? new List<RadarrQualityProfile>();
        }
        catch
        {
            return new List<RadarrQualityProfile>();
        }
    }

    private static HttpRequestMessage CreateRequest(HttpMethod method, string baseUrl, string path, string apiKey)
    {
        var url = path.StartsWith("http", StringComparison.OrdinalIgnoreCase) ? path : baseUrl + "/" + path.TrimStart('/');
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Api-Key", apiKey);
        return request;
    }
}

internal class RadarrRootFolder
{
    public string Path { get; set; } = string.Empty;
}

internal class RadarrQualityProfile
{
    public int Id { get; set; }
}
