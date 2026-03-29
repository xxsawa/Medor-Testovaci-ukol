using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Medor.Web.Pages;

/// <summary>
/// Razor page for listing saved rows, editing notes, chart, and delete (client-driven via JS).
/// </summary>
public class SavedModel : PageModel
{
    /// <summary>GET: static markup; data is loaded by the Saved page script from the API.</summary>
    public void OnGet()
    {
    }
}
