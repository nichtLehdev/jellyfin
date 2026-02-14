#nullable disable
#pragma warning disable CS1591

namespace Jellyfin.ArrIntegration.Configuration;

/// <summary>
/// Configuration for Radarr and Sonarr API integration.
/// </summary>
public class ArrIntegrationOptions
{
    /// <summary>
    /// Gets or sets the Radarr base URL (e.g. http://localhost:7878).
    /// </summary>
    public string RadarrBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Radarr API key.
    /// </summary>
    public string RadarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether Radarr integration is enabled.
    /// </summary>
    public bool RadarrEnabled { get; set; }

    /// <summary>
    /// Gets or sets the Sonarr base URL (e.g. http://localhost:8989).
    /// </summary>
    public string SonarrBaseUrl { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the Sonarr API key.
    /// </summary>
    public string SonarrApiKey { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether Sonarr integration is enabled.
    /// </summary>
    public bool SonarrEnabled { get; set; }
}
