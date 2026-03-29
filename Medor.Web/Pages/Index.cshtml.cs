using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Medor.Web.Pages;

/// <summary>
/// Home page: redirects visitors to the live BTC price page.
/// </summary>
public class IndexModel : PageModel
{
    /// <summary>Redirects to <c>/Live</c>.</summary>
    public IActionResult OnGet() => RedirectToPage("/Live");
}
