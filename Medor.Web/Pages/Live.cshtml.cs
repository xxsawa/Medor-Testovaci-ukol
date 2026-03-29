using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Medor.Web.Pages;

/// <summary>
/// Razor page for live CoinDesk + ČNB data, chart, and saving snapshots (client-driven via JS).
/// </summary>
public class LiveModel : PageModel
{
    /// <summary>GET: static markup; data is loaded by the Live page script from the API.</summary>
    public void OnGet()
    {
    }
}
