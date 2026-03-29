using Microsoft.EntityFrameworkCore;

namespace Medor.Api.Data;

/// <summary>
/// EF Core database context for persisted Bitcoin price snapshots and notes.
/// </summary>
/// <param name="options">Configured options for this context.</param>
public class MedorDbContext(DbContextOptions<MedorDbContext> options) : DbContext(options)
{
    /// <summary>Stored BTC price rows (live snapshots saved by users).</summary>
    public DbSet<BitcoinPriceRecord> BitcoinPrices => Set<BitcoinPriceRecord>();

    /// <inheritdoc />
    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        var e = modelBuilder.Entity<BitcoinPriceRecord>();
        e.ToTable("BitcoinPrices");
        e.HasKey(x => x.Id);
        e.Property(x => x.BtcEur).HasPrecision(18, 8);
        e.Property(x => x.BtcCzk).HasPrecision(18, 2);
        e.Property(x => x.EurCzkRate).HasPrecision(18, 6);
        e.Property(x => x.Note).HasMaxLength(500).IsRequired();
    }
}
