namespace Medor.Api.Contracts;

/// <summary>
/// Chart.js–friendly series: x-axis labels, BTC/CZK values, and matching row notes in fetch order.
/// </summary>
/// <param name="Labels">UTC ISO 8601 timestamps (one per point) for chart axis/tooltip parsing.</param>
/// <param name="BtcCzk">Matching BTC/CZK values.</param>
/// <param name="Notes">User note per point (same order as labels).</param>
public sealed record ChartSeries(IReadOnlyList<string> Labels, IReadOnlyList<decimal> BtcCzk, IReadOnlyList<string> Notes);
