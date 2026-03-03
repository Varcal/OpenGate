using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using OpenIddict.Server.AspNetCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Connect;

[ValidateAntiForgeryToken]
public sealed partial class LogoutModel(
    SignInManager<OpenGateUser> signInManager,
    ILogger<LogoutModel> logger) : PageModel
{
    public IActionResult OnGet()
    {
        // GET shows the confirmation page (already rendered by the .cshtml).
        // If the user is not authenticated there's nothing to do — redirect home.
        if (!User.Identity?.IsAuthenticated == true)
        {
            return Redirect("/");
        }

        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        // Try to read the OpenIddict end-session request to honour post_logout_redirect_uri.
        var oidcRequest = HttpContext.GetOpenIddictServerRequest();

        await signInManager.SignOutAsync();
        Log.UserSignedOut(logger);

        // If there is an OpenIddict end-session request in context, complete it
        // by signing out of the OpenIddict scheme so the server can redirect the client.
        if (oidcRequest is not null)
        {
            return SignOut(
                authenticationSchemes: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
                properties: new AuthenticationProperties
                {
                    RedirectUri = oidcRequest.PostLogoutRedirectUri ?? "/"
                });
        }

        return Redirect("/");
    }

    private static partial class Log
    {
        [LoggerMessage(Level = LogLevel.Information, Message = "User signed out.")]
        public static partial void UserSignedOut(ILogger logger);
    }
}

