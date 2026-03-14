using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using OpenGate.Admin.Api.Security;

namespace OpenGate.UI.Pages.Admin;

[Authorize(Policy = OpenGateAdminPolicies.Viewer)]
public abstract class AdminPageModel : PageModel
{
    [TempData]
    public string? StatusMessage { get; set; }

    [TempData]
    public string? ErrorMessage { get; set; }
}

[Authorize(Policy = OpenGateAdminPolicies.Admin)]
public abstract class AdminWritePageModel : AdminPageModel
{
}