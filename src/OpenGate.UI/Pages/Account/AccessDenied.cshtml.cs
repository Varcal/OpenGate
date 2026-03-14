using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace OpenGate.UI.Pages.Account;

public sealed class AccessDeniedModel : PageModel
{
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    public void OnGet()
    {
        ReturnUrl ??= string.Empty;
    }
}