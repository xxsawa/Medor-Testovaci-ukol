using Medor.Api.Services;
using Microsoft.AspNetCore.Mvc;

namespace Medor.Api.Controllers;

/// <summary>
/// Exposes the current live BTC/EUR and BTC/CZK snapshot (CoinDesk + ČNB).
/// </summary>
/// <param name="live">Domain service for live prices.</param>
[ApiController]
[Route("api/[controller]")]
public sealed class LivePriceController(ILiveBitcoinPriceService live) : ControllerBase
{
    /// <summary>Gets the latest computed live price DTO.</summary>
    /// <param name="cancellationToken">Propagates client disconnect.</param>
    [HttpGet]
    public async Task<IActionResult> Get(CancellationToken cancellationToken)
    {
        var dto = await live.GetLiveAsync(cancellationToken).ConfigureAwait(false);
        return Ok(dto);
    }
}
