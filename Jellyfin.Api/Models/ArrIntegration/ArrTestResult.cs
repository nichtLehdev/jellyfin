#pragma warning disable CS1591

namespace Jellyfin.Api.Models.ArrIntegration;

/// <summary>
/// Result of an Arr connection test.
/// </summary>
public class ArrTestResult
{
    /// <summary>Gets or sets a value indicating whether the connection succeeded.</summary>
    public bool Success { get; set; }
}
