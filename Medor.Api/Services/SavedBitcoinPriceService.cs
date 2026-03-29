using Medor.Api.Contracts;
using Medor.Api.Data;
using Medor.Api.Persistence;

namespace Medor.Api.Services;

/// <summary>
/// Orchestrates live snapshots and saved prices; persistence goes through <see cref="IBitcoinPriceRepository"/>.
/// </summary>
/// <param name="prices">Database access for Bitcoin price rows.</param>
/// <param name="livePrices">Used when saving the current live snapshot.</param>
public sealed class SavedBitcoinPriceService(
    IBitcoinPriceRepository prices,
    ILiveBitcoinPriceService livePrices) : ISavedBitcoinPriceService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<SavedPriceRow>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var rows = await prices.GetAllOrderedByFetchedDescAsync(cancellationToken).ConfigureAwait(false);
        return rows.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task<SavedPriceRow> SaveCurrentAsync(string note, CancellationToken cancellationToken = default)
    {
        var snapshot = await livePrices.GetLiveAsync(cancellationToken).ConfigureAwait(false);
        var entity = new BitcoinPriceRecord
        {
            BtcEur = snapshot.BtcEur,
            BtcCzk = snapshot.BtcCzk,
            EurCzkRate = snapshot.EurCzkRate,
            CnbRateValidFor = snapshot.CnbRateValidFor,
            FetchedAtUtc = snapshot.FetchedAtUtc,
            Note = note.Trim()
        };
        await prices.AddAsync(entity, cancellationToken).ConfigureAwait(false);
        return Map(entity);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<SavedPriceRow>> SaveSnapshotsAsync(
        IReadOnlyList<PriceSnapshotItem> snapshots,
        string note,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(snapshots);
        if (snapshots.Count == 0)
            throw new ArgumentException("Snapshots must not be empty.", nameof(snapshots));

        var trimmed = note.Trim();
        var entities = snapshots
            .Select(
                x => new BitcoinPriceRecord
                {
                    BtcEur = x.BtcEur,
                    BtcCzk = x.BtcCzk,
                    EurCzkRate = x.EurCzkRate,
                    CnbRateValidFor = x.CnbRateValidFor,
                    FetchedAtUtc = x.FetchedAtUtc,
                    Note = trimmed,
                })
            .ToList();
        await prices.AddRangeAsync(entities, cancellationToken).ConfigureAwait(false);
        return entities.Select(Map).ToList();
    }

    /// <inheritdoc />
    public async Task UpdateNotesAsync(IReadOnlyList<NoteUpdate> items, CancellationToken cancellationToken = default)
    {
        var pairs = items.Select(x => (x.Id, x.Note.Trim())).ToList();
        await prices.UpdateNotesAsync(pairs, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> DeleteAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        return await prices.DeleteByIdsAsync(ids, cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<ChartSeries> GetChartSeriesAsync(CancellationToken cancellationToken = default)
    {
        var rows = await prices.GetChartPointsOrderedAsync(cancellationToken).ConfigureAwait(false);
        // Round-trip ISO so the web chart can parse labels for compact axis ticks and tooltips.
        var labels = rows.Select(x => x.FetchedAtUtc.ToUniversalTime().ToString("o")).ToList();
        var data = rows.Select(x => x.BtcCzk).ToList();
        var notes = rows.Select(x => x.Note).ToList();
        return new ChartSeries(labels, data, notes);
    }

    /// <summary>Maps an entity to an API DTO.</summary>
    private static SavedPriceRow Map(BitcoinPriceRecord x) =>
        new(x.Id, x.BtcEur, x.BtcCzk, x.EurCzkRate, x.CnbRateValidFor, x.FetchedAtUtc, x.Note);
}
