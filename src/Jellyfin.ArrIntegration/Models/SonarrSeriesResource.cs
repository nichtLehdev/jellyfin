#nullable disable
#pragma warning disable CS1591

using System.Text.Json.Serialization;

namespace Jellyfin.ArrIntegration.Models;

/// <summary>
/// Minimal DTO for Sonarr series lookup/add response.
/// </summary>
public class SonarrSeriesResource
{
    [JsonPropertyName("tvdbId")]
    public int TvdbId { get; set; }

    [JsonPropertyName("title")]
    public string Title { get; set; }

    [JsonPropertyName("id")]
    public int Id { get; set; }

    [JsonPropertyName("titleSlug")]
    public string TitleSlug { get; set; }

    [JsonPropertyName("images")]
    public SonarrImage[] Images { get; set; }

    [JsonPropertyName("monitored")]
    public bool Monitored { get; set; }

    [JsonPropertyName("qualityProfileId")]
    public int QualityProfileId { get; set; }

    [JsonPropertyName("rootFolderPath")]
    public string RootFolderPath { get; set; }

    [JsonPropertyName("addOptions")]
    public SonarrAddOptions AddOptions { get; set; }

    [JsonPropertyName("seasonFolder")]
    public bool SeasonFolder { get; set; } = true;
}

/// <summary>
/// Sonarr image.
/// </summary>
public class SonarrImage
{
    [JsonPropertyName("coverType")]
    public string CoverType { get; set; }

    [JsonPropertyName("url")]
    public string Url { get; set; }
}

/// <summary>
/// Sonarr add series options.
/// </summary>
public class SonarrAddOptions
{
    [JsonPropertyName("searchForMissingEpisodes")]
    public bool SearchForMissingEpisodes { get; set; } = true;

    [JsonPropertyName("monitor")]
    public string Monitor { get; set; } = "all";
}
