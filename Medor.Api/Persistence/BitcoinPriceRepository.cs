using Medor.Api.Data;
using Microsoft.EntityFrameworkCore;

namespace Medor.Api.Persistence;

/// <inheritdoc />
public sealed class BitcoinPriceRepository(MedorDbContext db) : IBitcoinPriceRepository
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<BitcoinPriceRecord>> GetAllOrderedByFetchedDescAsync(
        CancellationToken cancellationToken = default)
    {
        return await db.BitcoinPrices
            .AsNoTracking()
            .OrderByDescending(x => x.FetchedAtUtc)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddAsync(BitcoinPriceRecord entity, CancellationToken cancellationToken = default)
    {
        db.BitcoinPrices.Add(entity);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task AddRangeAsync(IReadOnlyList<BitcoinPriceRecord> entities, CancellationToken cancellationToken = default)
    {
        if (entities.Count == 0)
            return;
        db.BitcoinPrices.AddRange(entities);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task UpdateNotesAsync(
        IReadOnlyList<(int Id, string Note)> items,
        CancellationToken cancellationToken = default)
    {
        foreach (var item in items)
        {
            var row = await db.BitcoinPrices
                .FirstOrDefaultAsync(x => x.Id == item.Id, cancellationToken)
                .ConfigureAwait(false);
            if (row is null)
                continue;
            row.Note = item.Note;
        }

        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<int> DeleteByIdsAsync(IReadOnlyList<int> ids, CancellationToken cancellationToken = default)
    {
        if (ids.Count == 0)
            return 0;
        var set = ids.ToHashSet();
        var toRemove = await db.BitcoinPrices
            .Where(x => set.Contains(x.Id))
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        db.BitcoinPrices.RemoveRange(toRemove);
        await db.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        return toRemove.Count;
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<(DateTime FetchedAtUtc, decimal BtcCzk, string Note)>> GetChartPointsOrderedAsync(
        CancellationToken cancellationToken = default)
    {
        var rows = await db.BitcoinPrices
            .AsNoTracking()
            .OrderBy(x => x.FetchedAtUtc)
            .Select(x => new { x.FetchedAtUtc, x.BtcCzk, x.Note })
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);
        return rows.Select(x => (x.FetchedAtUtc, x.BtcCzk, x.Note)).ToList();
    }
}
