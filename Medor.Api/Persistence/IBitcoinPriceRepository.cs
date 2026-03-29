using Medor.Api.Data;

namespace Medor.Api.Persistence;

/// <summary>
/// Data access for <see cref="BitcoinPriceRecord"/> rows (no HTTP or business rules).
/// </summary>
public interface IBitcoinPriceRepository
{
    /// <summary>All rows, newest <see cref="BitcoinPriceRecord.FetchedAtUtc"/> first.</summary>
    Task<IReadOnlyList<BitcoinPriceRecord>> GetAllOrderedByFetchedDescAsync(CancellationToken cancellationToken = default);

    /// <summary>Persists a new row.</summary>
    Task AddAsync(BitcoinPriceRecord entity, CancellationToken cancellationToken = default);

    /// <summary>Persists multiple new rows in one transaction.</summary>
    Task AddRangeAsync(IReadOnlyList<BitcoinPriceRecord> entities, CancellationToken cancellationToken = default);

    /// <summary>Updates notes for the given ids (skips missing ids).</summary>
    Task UpdateNotesAsync(IReadOnlyList<(int Id, string Note)> items, CancellationToken cancellationToken = default);

    /// <summary>Deletes rows by primary keys; returns count removed.</summary>
    Task<int> DeleteByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default);

    /// <summary>Ordered points for charting BTC/CZK over time (includes note per row).</summary>
    Task<IReadOnlyList<(DateTime FetchedAtUtc, decimal BtcCzk, string Note)>> GetChartPointsOrderedAsync(
        CancellationToken cancellationToken = default);
}
