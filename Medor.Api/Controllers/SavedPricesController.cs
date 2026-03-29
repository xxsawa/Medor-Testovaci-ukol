using System.Diagnostics;
using Medor.Api.Contracts;
using Medor.Api.Services;
using Medor.Api.Validation;
using Microsoft.AspNetCore.Mvc;

namespace Medor.Api.Controllers;

/// <summary>
/// CRUD-style API for saved price rows, bulk note updates, and chart series for stored BTC/CZK history.
/// </summary>
/// <param name="saved">Persistence service for saved prices.</param>
[ApiController]
[Route("api/[controller]")]
public sealed class SavedPricesController(ISavedBitcoinPriceService saved) : ControllerBase
{
    /// <summary>Returns all saved rows, newest first.</summary>
    [HttpGet]
    public async Task<IActionResult> GetAll(CancellationToken cancellationToken)
    {
        var rows = await saved.GetAllAsync(cancellationToken).ConfigureAwait(false);
        return Ok(rows);
    }

    /// <summary>
    /// Returns labels and BTC/CZK points for a chart. Uses <see cref="CancellationToken.None"/> for EF so client abort does not cancel the query.
    /// </summary>
    [HttpGet("chart")]
    public async Task<IActionResult> Chart()
    {
        // Chart read must not use the request token (client abort cancels EF during OpenAsync).
        var series = await saved.GetChartSeriesAsync(CancellationToken.None).ConfigureAwait(false);
        return Ok(series);
    }

    /// <summary>
    /// Saves either the current live snapshot (default) or a batch of client-observed snapshots (<c>items</c>) with one shared note.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Save([FromBody] SavePriceRequest? body, CancellationToken cancellationToken)
    {
        if (!SavedPricesRequestValidator.TryValidateSave(body, out var validated, out var error))
            return BadRequest(error);

        switch (validated)
        {
            case SaveValidated.Batch(var note, var items):
                var rows = await saved.SaveSnapshotsAsync(items, note, cancellationToken).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status201Created, rows);
            case SaveValidated.Current(var note):
                var row = await saved.SaveCurrentAsync(note, cancellationToken).ConfigureAwait(false);
                return StatusCode(StatusCodes.Status201Created, row);
            default:
                throw new UnreachableException();
        }
    }

    /// <summary>Bulk-updates notes for selected ids (each note must be non-empty).</summary>
    [HttpPut("notes")]
    public async Task<IActionResult> UpdateNotes([FromBody] UpdateNotesRequest? body, CancellationToken cancellationToken)
    {
        if (!SavedPricesRequestValidator.TryValidateUpdateNotes(body, out var updates, out var error))
            return BadRequest(error);

        await saved.UpdateNotesAsync(updates, cancellationToken).ConfigureAwait(false);
        return NoContent();
    }

    /// <summary>Deletes rows by id list.</summary>
    [HttpDelete]
    public async Task<IActionResult> Delete([FromBody] DeletePricesRequest? body, CancellationToken cancellationToken)
    {
        if (!SavedPricesRequestValidator.TryValidateDelete(body, out var ids, out var error))
            return BadRequest(error);

        var removed = await saved.DeleteAsync(ids, cancellationToken).ConfigureAwait(false);
        return Ok(new { deleted = removed });
    }
}
