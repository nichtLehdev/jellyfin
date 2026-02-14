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
/// HTTP client for Sonarr API.
/// </summary>
public class SonarrApiService : ISonarrApiService
{
    private readonly IConfigurationManager _config;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<SonarrApiService> _logger;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Initializes a new instance of the <see cref="SonarrApiService"/> class.
    /// </summary>
    public SonarrApiService(
        IConfigurationManager config,
        IHttpClientFactory httpClientFactory,
        ILogger<SonarrApiService> logger)
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
            _logger.LogWarning(ex, "Sonarr connection test failed for {BaseUrl}", baseUrl);
            return false;
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SonarrSeriesResource>> LookupAsync(string term, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.SonarrEnabled || string.IsNullOrWhiteSpace(options.SonarrBaseUrl) || string.IsNullOrWhiteSpace(options.SonarrApiKey))
        {
            return Array.Empty<SonarrSeriesResource>();
        }

        var baseUrl = options.SonarrBaseUrl.TrimEnd('/');
        var path = "api/v3/series/lookup?term=" + Uri.EscapeDataString(term);
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, path, options.SonarrApiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<SonarrSeriesResource>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return (IReadOnlyList<SonarrSeriesResource>)(list ?? new List<SonarrSeriesResource>());
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sonarr lookup failed for term {Term}", term);
            return Array.Empty<SonarrSeriesResource>();
        }
    }

    /// <inheritdoc />
    public async Task<SonarrSeriesResource> AddSeriesAsync(SonarrSeriesResource series, bool searchForMissingEpisodes = true, CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.SonarrEnabled || string.IsNullOrWhiteSpace(options.SonarrBaseUrl) || string.IsNullOrWhiteSpace(options.SonarrApiKey))
        {
            throw new InvalidOperationException("Sonarr is not configured or enabled.");
        }

        series.AddOptions ??= new SonarrAddOptions
        {
            SearchForMissingEpisodes = searchForMissingEpisodes,
            Monitor = "all"
        };
        series.Monitored = true;
        var baseUrl = options.SonarrBaseUrl.TrimEnd('/');
        var apiKey = options.SonarrApiKey;

        if (series.QualityProfileId <= 0)
        {
            var profiles = await GetQualityProfilesAsync(baseUrl, apiKey, cancellationToken).ConfigureAwait(false);
            series.QualityProfileId = profiles.Count > 0 ? profiles[0].Id : 1;
        }

        if (string.IsNullOrWhiteSpace(series.RootFolderPath))
        {
            var folders = await GetRootFoldersAsync(baseUrl, apiKey, cancellationToken).ConfigureAwait(false);
            if (folders.Count > 0)
            {
                series.RootFolderPath = folders[0].Path;
            }
        }

        using var request = CreateRequest(HttpMethod.Post, baseUrl, "api/v3/series", apiKey);
        request.Content = JsonContent.Create(series, options: JsonOptions);
        using var client = _httpClientFactory.CreateClient();
        using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
        response.EnsureSuccessStatusCode();
        var added = await response.Content.ReadFromJsonAsync<SonarrSeriesResource>(JsonOptions, cancellationToken).ConfigureAwait(false);
        if (added is null)
        {
            throw new InvalidOperationException("Sonarr returned no series data.");
        }

        return added;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<ArrQueueItem>> GetQueueAsync(CancellationToken cancellationToken = default)
    {
        var options = GetOptions();
        if (!options.SonarrEnabled || string.IsNullOrWhiteSpace(options.SonarrBaseUrl) || string.IsNullOrWhiteSpace(options.SonarrApiKey))
        {
            return Array.Empty<ArrQueueItem>();
        }

        var baseUrl = options.SonarrBaseUrl.TrimEnd('/');
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/queue?pageSize=100&includeSeries=true&includeEpisode=true", options.SonarrApiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var queue = await response.Content.ReadFromJsonAsync<SonarrQueueResponse>(JsonOptions, cancellationToken).ConfigureAwait(false);
            if (queue?.Records is null || queue.Records.Length == 0)
            {
                return Array.Empty<ArrQueueItem>();
            }

            var list = new List<ArrQueueItem>(queue.Records.Length);
            foreach (var r in queue.Records)
            {
                list.Add(new ArrQueueItem
                {
                    Source = "Sonarr",
                    Id = r.Id,
                    Title = r.Series?.Title ?? r.Episode?.Title ?? r.Title ?? "Unknown",
                    Status = r.Status ?? string.Empty,
                    Progress = r.Progress,
                    Size = r.Size,
                    SizeLeft = r.SizeLeft,
                    TimeLeft = r.TimeLeft ?? string.Empty,
                    EstimatedCompletionTime = r.EstimatedCompletionTime ?? string.Empty,
                    DownloadId = r.DownloadId ?? string.Empty,
                    Series = r.Series is null ? null : new ArrQueueSeriesInfo { Title = r.Series.Title },
                    Episode = r.Episode is null ? null : new ArrQueueEpisodeInfo
                    {
                        SeasonNumber = r.Episode.SeasonNumber,
                        EpisodeNumber = r.Episode.EpisodeNumber,
                        Title = r.Episode.Title ?? string.Empty
                    }
                });
            }

            return list;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Sonarr get queue failed.");
            return Array.Empty<ArrQueueItem>();
        }
    }

    private async Task<List<SonarrRootFolder>> GetRootFoldersAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/rootfolder", apiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<SonarrRootFolder>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return list ?? new List<SonarrRootFolder>();
        }
        catch
        {
            return new List<SonarrRootFolder>();
        }
    }

    private async Task<List<SonarrQualityProfile>> GetQualityProfilesAsync(string baseUrl, string apiKey, CancellationToken cancellationToken)
    {
        try
        {
            using var request = CreateRequest(HttpMethod.Get, baseUrl, "api/v3/qualityprofile", apiKey);
            using var client = _httpClientFactory.CreateClient();
            using var response = await client.SendAsync(request, cancellationToken).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();
            var list = await response.Content.ReadFromJsonAsync<List<SonarrQualityProfile>>(JsonOptions, cancellationToken).ConfigureAwait(false);
            return list ?? new List<SonarrQualityProfile>();
        }
        catch
        {
            return new List<SonarrQualityProfile>();
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

internal class SonarrRootFolder
{
    public string Path { get; set; } = string.Empty;
}

internal class SonarrQualityProfile
{
    public int Id { get; set; }
}
