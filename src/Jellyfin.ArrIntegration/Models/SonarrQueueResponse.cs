#nullable disable
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace Jellyfin.ArrIntegration.Models;

/// <summary>
/// Sonarr queue API response.
/// </summary>
public class SonarrQueueResponse
{
    [JsonPropertyName("records")]
    public SonarrQueueRecord[] Records { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }
}

/// <summary>
/// Single Sonarr queue record.
/// </summary>
public class SonarrQueueRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("seriesId")]
    public int SeriesId { get; set; }

    [JsonPropertyName("episodeId")]
    public int EpisodeId { get; set; }

    /// <summary>
    /// Gets or sets the top-level title (from download client); populated when includeSeries/includeEpisode are false or as fallback.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("series")]
    public SonarrQueueSeries Series { get; set; }

    [JsonPropertyName("episode")]
    public SonarrQueueEpisode Episode { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }

    [JsonPropertyName("sizeleft")]
    public long? SizeLeft { get; set; }

    [JsonPropertyName("timeleft")]
    public string TimeLeft { get; set; }

    [JsonPropertyName("estimatedCompletionTime")]
    public string EstimatedCompletionTime { get; set; }

    [JsonPropertyName("downloadId")]
    public string DownloadId { get; set; }

    [JsonPropertyName("progress")]
    public double? Progress { get; set; }
}

/// <summary>
/// Series in Sonarr queue.
/// </summary>
public class SonarrQueueSeries
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
}

/// <summary>
/// Episode in Sonarr queue.
/// </summary>
public class SonarrQueueEpisode
{
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
