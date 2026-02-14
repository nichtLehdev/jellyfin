#nullable disable
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace Jellyfin.ArrIntegration.Models;

/// <summary>
/// Unified queue item from Radarr or Sonarr.
/// </summary>
public class ArrQueueItem
{
    [JsonPropertyName("source")]
    public string Source { get; set; } = "unknown";

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("status")]
    public string Status { get; set; }

    [JsonPropertyName("progress")]
    public double? Progress { get; set; }

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

    [JsonPropertyName("movie")]
    public ArrQueueMovieInfo Movie { get; set; }

    [JsonPropertyName("series")]
    public ArrQueueSeriesInfo Series { get; set; }

    [JsonPropertyName("episode")]
    public ArrQueueEpisodeInfo Episode { get; set; }
}

/// <summary>
/// Movie info in queue (Radarr).
/// </summary>
public class ArrQueueMovieInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}

/// <summary>
/// Series info in queue (Sonarr).
/// </summary>
public class ArrQueueSeriesInfo
{
    [JsonPropertyName("title")]
    public string Title { get; set; }
}

/// <summary>
/// Episode info in queue (Sonarr).
/// </summary>
public class ArrQueueEpisodeInfo
{
    [JsonPropertyName("seasonNumber")]
    public int SeasonNumber { get; set; }

    [JsonPropertyName("episodeNumber")]
    public int EpisodeNumber { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }
}
