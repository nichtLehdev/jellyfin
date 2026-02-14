#pragma warning disable CS1591
#pragma warning disable SA1611 // Missing parameter documentation
#pragma warning disable SA1615 // Element return value should be documented

using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Api.Models.ArrIntegration;
using Jellyfin.ArrIntegration.Configuration;
using Jellyfin.ArrIntegration.Models;
using Jellyfin.ArrIntegration.Services;
using MediaBrowser.Common.Api;
using MediaBrowser.Controller.Configuration;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Jellyfin.Api.Controllers;

/// <summary>
/// Radarr and Sonarr integration controller.
/// </summary>
[Route("ArrIntegration")]
[Authorize]
public class ArrIntegrationController : BaseJellyfinApiController
{
    private readonly IServerConfigurationManager _config;
    private readonly IRadarrApiService _radarr;
    private readonly ISonarrApiService _sonarr;

    /// <summary>
    /// Initializes a new instance of the <see cref="ArrIntegrationController"/> class.
    /// </summary>
    /// <param name="config">The server configuration manager.</param>
    /// <param name="radarr">The Radarr API service.</param>
    /// <param name="sonarr">The Sonarr API service.</param>
    public ArrIntegrationController(
        IServerConfigurationManager config,
        IRadarrApiService radarr,
        ISonarrApiService sonarr)
    {
        _config = config;
        _radarr = radarr;
        _sonarr = sonarr;
    }

    /// <summary>
    /// Gets Radarr and Sonarr connection settings.
    /// </summary>
    /// <response code="200">Settings returned.</response>
    [HttpGet("Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public ActionResult<ArrIntegrationOptions> GetSettings()
    {
        var options = _config.GetConfiguration("arrintegration") as ArrIntegrationOptions
            ?? new ArrIntegrationOptions();
        return options;
    }

    /// <summary>
    /// Updates Radarr and Sonarr connection settings.
    /// </summary>
    /// <param name="options">The settings to save.</param>
    /// <response code="204">Settings updated.</response>
    [HttpPost("Settings")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    public ActionResult UpdateSettings([FromBody, Required] ArrIntegrationOptions options)
    {
        _config.SaveConfiguration("arrintegration", options);
        return NoContent();
    }

    /// <summary>
    /// Tests connection to Radarr.
    /// </summary>
    /// <param name="request">Base URL and API key to test.</param>
    /// <response code="200">Connection test result.</response>
    [HttpPost("Radarr/Test")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArrTestResult>> TestRadarrAsync([FromBody, Required] ArrTestRequest request)
    {
        var ok = await _radarr.TestConnectionAsync(
            request.BaseUrl ?? string.Empty,
            request.ApiKey ?? string.Empty,
            CancellationToken.None).ConfigureAwait(false);
        return new ArrTestResult { Success = ok };
    }

    /// <summary>
    /// Tests connection to Sonarr.
    /// </summary>
    /// <param name="request">Base URL and API key to test.</param>
    /// <response code="200">Connection test result.</response>
    [HttpPost("Sonarr/Test")]
    [Authorize(Policy = Policies.RequiresElevation)]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<ArrTestResult>> TestSonarrAsync([FromBody, Required] ArrTestRequest request)
    {
        var ok = await _sonarr.TestConnectionAsync(
            request.BaseUrl ?? string.Empty,
            request.ApiKey ?? string.Empty,
            CancellationToken.None).ConfigureAwait(false);
        return new ArrTestResult { Success = ok };
    }

    /// <summary>
    /// Gets the combined download queue from Radarr and Sonarr.
    /// </summary>
    /// <response code="200">Queue items returned.</response>
    [HttpGet("Downloads")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<ArrQueueItem>>> GetDownloadsAsync()
    {
        var options = _config.GetConfiguration("arrintegration") as ArrIntegrationOptions;
        var list = new List<ArrQueueItem>();
        if (options?.RadarrEnabled == true)
        {
            var r = await _radarr.GetQueueAsync(CancellationToken.None).ConfigureAwait(false);
            list.AddRange(r);
        }

        if (options?.SonarrEnabled == true)
        {
            var s = await _sonarr.GetQueueAsync(CancellationToken.None).ConfigureAwait(false);
            list.AddRange(s);
        }

        return list;
    }

    /// <summary>
    /// Searches for movies in Radarr (discover / not yet in library).
    /// </summary>
    /// <param name="term">Search term.</param>
    /// <response code="200">Movies returned.</response>
    [HttpGet("Radarr/Movies/Lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<RadarrMovieResource>>> LookupRadarrMoviesAsync([FromQuery, Required] string term)
    {
        var results = await _radarr.LookupAsync(term, CancellationToken.None).ConfigureAwait(false);
        return Ok(results.AsEnumerable());
    }

    /// <summary>
    /// Adds a movie to Radarr (monitor and optionally start download).
    /// </summary>
    /// <param name="movie">Movie from lookup to add.</param>
    /// <param name="searchForMovie">Whether to search for a release immediately.</param>
    /// <response code="200">Movie added.</response>
    [HttpPost("Radarr/Movies")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<RadarrMovieResource>> AddRadarrMovieAsync([FromBody, Required] RadarrMovieResource movie, [FromQuery] bool searchForMovie = true)
    {
        try
        {
            var added = await _radarr.AddMovieAsync(movie, searchForMovie, CancellationToken.None).ConfigureAwait(false);
            return added;
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }

    /// <summary>
    /// Searches for series in Sonarr (discover / not yet in library).
    /// </summary>
    /// <param name="term">Search term.</param>
    /// <response code="200">Series returned.</response>
    [HttpGet("Sonarr/Series/Lookup")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    public async Task<ActionResult<IEnumerable<SonarrSeriesResource>>> LookupSonarrSeriesAsync([FromQuery, Required] string term)
    {
        var results = await _sonarr.LookupAsync(term, CancellationToken.None).ConfigureAwait(false);
        return Ok(results.AsEnumerable());
    }

    /// <summary>
    /// Adds a series to Sonarr (monitor and optionally search for episodes).
    /// </summary>
    /// <param name="series">Series from lookup to add.</param>
    /// <param name="searchForMissingEpisodes">Whether to search for missing episodes immediately.</param>
    /// <response code="200">Series added.</response>
    [HttpPost("Sonarr/Series")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<SonarrSeriesResource>> AddSonarrSeriesAsync([FromBody, Required] SonarrSeriesResource series, [FromQuery] bool searchForMissingEpisodes = true)
    {
        try
        {
            var added = await _sonarr.AddSeriesAsync(series, searchForMissingEpisodes, CancellationToken.None).ConfigureAwait(false);
            return added;
        }
        catch (System.InvalidOperationException ex)
        {
            return BadRequest(ex.Message);
        }
    }
}
