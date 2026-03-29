namespace Medor.Api.Contracts;

/// <summary>
/// One client-supplied price snapshot (e.g. from the Live page history) for batch insert.
/// </summary>
public sealed class PriceSnapshotItem
{
    public decimal BtcEur { get; set; }

    public decimal BtcCzk { get; set; }

    public decimal EurCzkRate { get; set; }

    public DateOnly CnbRateValidFor { get; set; }

    public DateTime FetchedAtUtc { get; set; }
}
