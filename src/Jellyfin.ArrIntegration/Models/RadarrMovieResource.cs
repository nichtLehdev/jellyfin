#nullable disable
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace Jellyfin.ArrIntegration.Models;

/// <summary>
/// Minimal DTO for Radarr movie lookup/add response.
/// </summary>
public class RadarrMovieResource
{
    [JsonPropertyName("tmdbId")]
    public int TmdbId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("year")]
    public int Year { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("titleSlug")]
    public string TitleSlug { get; set; }

    [JsonPropertyName("images")]
    public RadarrImage[] Images { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    [JsonPropertyName("rootFolderPath")]
    public string RootFolderPath { get; set; }

    [JsonPropertyName("addOptions")]
    public RadarrAddOptions AddOptions { get; set; }
}

/// <summary>
/// Radarr image.
/// </summary>
public class RadarrImage
{
    [JsonPropertyName("coverType")]
    public string CoverType { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

/// <summary>
/// Radarr add movie options.
/// </summary>
public class RadarrAddOptions
{
    [JsonPropertyName("searchForMovie")]
    public bool SearchForMovie { get; set; } = true;
}
