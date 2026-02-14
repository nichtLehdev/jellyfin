#nullable disable
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace Jellyfin.ArrIntegration.Models;

/// <summary>
/// Radarr queue API response.
/// </summary>
public class RadarrQueueResponse
{
    [JsonPropertyName("records")]
    public RadarrQueueRecord[] Records { get; set; }

    [JsonPropertyName("totalRecords")]
    public int TotalRecords { get; set; }
}

/// <summary>
/// Single Radarr queue record.
/// </summary>
public class RadarrQueueRecord
{
    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("movieId")]
    public int MovieId { get; set; }

    /// <summary>
    /// Gets or sets the top-level title (from download client); populated when includeMovie is false or as fallback.
    /// </summary>
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("movie")]
    public RadarrQueueMovie Movie { get; set; }

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
/// Movie in Radarr queue.
/// </summary>
public class RadarrQueueMovie
{
    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }
}
