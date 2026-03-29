using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Medor.Web.Pages;

/// <summary>
/// Privacy policy placeholder page from the default Razor template.
/// </summary>
public class PrivacyModel : PageModel
{
    private readonly ILogger<PrivacyModel> _logger;

    /// <summary>Creates the page with a logger instance.</summary>
    /// <param name="logger">Application logger.</param>
    public PrivacyModel(ILogger<PrivacyModel> logger)
    {
        _logger = logger;
    }

    /// <summary>GET: displays the privacy view.</summary>
    public void OnGet()
    {
    }
}

