using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin.Clients;

public sealed class EditModel(
    IOpenIddictApplicationManager applicationManager,
    OpenGateDbContext db) : AdminWritePageModel
{
    [BindProperty]
    public ClientFormInput Input { get; set; } = new();

    public IReadOnlyList<string> SupportedClientTypes { get; } = AdminOpenIddictManagementSupport.SupportedClientTypes;
    public IReadOnlyList<string> SupportedConsentTypes { get; } = AdminOpenIddictManagementSupport.SupportedConsentTypes;

    public async Task<IActionResult> OnGetAsync(string clientId, CancellationToken cancellationToken)
    {
        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, application, cancellationToken);
        Input = new ClientFormInput
        {
            ClientId = descriptor.ClientId,
            DisplayName = descriptor.DisplayName,
            ClientType = descriptor.ClientType,
            ConsentType = descriptor.ConsentType,
            RedirectUris = AdminOpenIddictManagementSupport.ToTextBlock(descriptor.RedirectUris.Select(uri => uri.AbsoluteUri)),
            PostLogoutRedirectUris = AdminOpenIddictManagementSupport.ToTextBlock(descriptor.PostLogoutRedirectUris.Select(uri => uri.AbsoluteUri)),
            Permissions = AdminOpenIddictManagementSupport.ToTextBlock(descriptor.Permissions),
            Requirements = AdminOpenIddictManagementSupport.ToTextBlock(descriptor.Requirements)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string clientId, CancellationToken cancellationToken)
    {
        AdminOpenIddictManagementSupport.ValidateClientInput(ModelState, Input, isCreate: false, routeClientId: clientId);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        await applicationManager.PopulateAsync(descriptor, application, cancellationToken);
        AdminOpenIddictManagementSupport.ApplyClientInput(descriptor, Input, isCreate: false);
        await applicationManager.UpdateAsync(application, descriptor, cancellationToken);

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ClientUpdated",
            new { clientId, Input.DisplayName, Source = "AdminUi.Edit" }));
        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Client {clientId} atualizado com sucesso.";
        return RedirectToPage("/Admin/Clients");
    }
}