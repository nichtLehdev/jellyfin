#pragma warning disable CS1591

namespace Jellyfin.Api.Models.ArrIntegration;

/// <summary>
/// Request body for testing Arr connection.
/// </summary>
public class ArrTestRequest
{
    /// <summary>Gets or sets the base URL (e.g. http://localhost:7878).</summary>
    public string BaseUrl { get; set; } = string.Empty;

    /// <summary>Gets or sets the API key.</summary>
    public string ApiKey { get; set; } = string.Empty;
}
