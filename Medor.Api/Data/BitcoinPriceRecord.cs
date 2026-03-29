namespace Medor.Api.Data;

/// <summary>
/// One saved row: snapshot of BTC/EUR, BTC/CZK, EUR/CZK from ČNB, and user note.
/// </summary>
public class BitcoinPriceRecord
{
    /// <summary>Primary key.</summary>
    public int Id { get; set; }

    /// <summary>BTC price in EUR at fetch time.</summary>
    public decimal BtcEur { get; set; }

    /// <summary>BTC price in CZK (derived from EUR cross).</summary>
    public decimal BtcCzk { get; set; }

    /// <summary>EUR/CZK rate (CZK per 1 EUR) used for conversion.</summary>
    public decimal EurCzkRate { get; set; }

    /// <summary>ČNB rate validity date for the EUR quote.</summary>
    public DateOnly CnbRateValidFor { get; set; }

    /// <summary>UTC timestamp when the snapshot was taken.</summary>
    public DateTime FetchedAtUtc { get; set; }

    /// <summary>Required user note (max length enforced in <see cref="MedorDbContext"/>).</summary>
    public string Note { get; set; } = string.Empty;
}
