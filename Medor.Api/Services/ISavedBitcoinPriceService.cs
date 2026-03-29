using Medor.Api.Contracts;

namespace Medor.Api.Services;

/// <summary>
/// Persistence and queries for user-saved price snapshots stored in SQL Server.
/// </summary>
public interface ISavedBitcoinPriceService
{
    /// <summary>All rows ordered by fetch time descending.</summary>
    Task<IReadOnlyList<SavedPriceRow>> GetAllAsync(CancellationToken cancellationToken = default);

    /// <summary>Fetches live prices, persists a new row with the given note.</summary>
    Task<SavedPriceRow> SaveCurrentAsync(string note, CancellationToken cancellationToken = default);

    /// <summary>Persists multiple client-observed snapshots with the same note (batch from Live history).</summary>
    Task<IReadOnlyList<SavedPriceRow>> SaveSnapshotsAsync(
        IReadOnlyList<PriceSnapshotItem> snapshots,
        string note,
        CancellationToken cancellationToken = default);

    /// <summary>Updates notes for the given row ids.</summary>
    Task UpdateNotesAsync(IReadOnlyList<NoteUpdate> items, CancellationToken cancellationToken = default);

    /// <summary>Deletes rows matching the ids; returns count removed.</summary>
    Task<int> DeleteAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Builds ordered chart series from stored BTC/CZK values.</summary>
    Task<ChartSeries> GetChartSeriesAsync(CancellationToken cancellationToken = default);
}

/// <summary>Id and new note text for a bulk note update.</summary>
/// <param name="Id">Row primary key.</param>
/// <param name="Note">Trimmed note text.</param>
public sealed record NoteUpdate(int Id, string Note);
