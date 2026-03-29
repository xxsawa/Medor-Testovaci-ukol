using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Medor.Web.Pages;

/// <summary>
/// Error page model: exposes a request/correlation id for diagnostics.
/// </summary>
[ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
[IgnoreAntiforgeryToken]
public class ErrorModel : PageModel
{
    /// <summary>Activity or trace id for this request.</summary>
    public string? RequestId { get; set; }

    /// <summary>Whether <see cref="RequestId"/> should be shown in the UI.</summary>
    public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);

    private readonly ILogger<ErrorModel> _logger;

    /// <summary>Creates the error page model.</summary>
    /// <param name="logger">Application logger.</param>
    public ErrorModel(ILogger<ErrorModel> logger)
    {
        _logger = logger;
    }

    /// <summary>GET: populates <see cref="RequestId"/> from activity or trace.</summary>
    public void OnGet()
    {
        RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier;
    }
}

