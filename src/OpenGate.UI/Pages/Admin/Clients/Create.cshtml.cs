using Microsoft.AspNetCore.Mvc;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin.Clients;

public sealed class CreateModel(
    IOpenIddictApplicationManager applicationManager,
    OpenGateDbContext db) : AdminWritePageModel
{
    [BindProperty]
    public ClientFormInput Input { get; set; } = new()
    {
        ClientType = AdminOpenIddictManagementSupport.NormalizeClientType(null),
        ConsentType = AdminOpenIddictManagementSupport.NormalizeConsentType(null)
    };

    public IReadOnlyList<string> SupportedClientTypes { get; } = AdminOpenIddictManagementSupport.SupportedClientTypes;
    public IReadOnlyList<string> SupportedConsentTypes { get; } = AdminOpenIddictManagementSupport.SupportedConsentTypes;

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        AdminOpenIddictManagementSupport.ValidateClientInput(ModelState, Input, isCreate: true);

        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await applicationManager.FindByClientIdAsync(Input.ClientId!, cancellationToken) is not null)
        {
            ModelState.AddModelError(nameof(Input.ClientId), "Já existe um client com este ClientId.");
            return Page();
        }

        var descriptor = new OpenIddictApplicationDescriptor();
        AdminOpenIddictManagementSupport.ApplyClientInput(descriptor, Input, isCreate: true);
        await applicationManager.CreateAsync(descriptor, cancellationToken);

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ClientCreated",
            new { Input.ClientId, Input.DisplayName, Source = "AdminUi.Create" }));
        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Client {Input.ClientId} criado com sucesso.";
        return RedirectToPage("/Admin/Clients");
    }
}