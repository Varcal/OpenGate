using Microsoft.AspNetCore.Mvc;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin.Scopes;

public sealed class CreateModel(
    IOpenIddictScopeManager scopeManager,
    OpenGateDbContext db) : AdminWritePageModel
{
    [BindProperty]
    public ScopeFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnPostAsync(CancellationToken cancellationToken)
    {
        AdminOpenIddictManagementSupport.ValidateScopeInput(ModelState, Input, isCreate: true);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        if (await scopeManager.FindByNameAsync(Input.Name!, cancellationToken) is not null)
        {
            ModelState.AddModelError(nameof(Input.Name), "Já existe um scope com este nome.");
            return Page();
        }

        var descriptor = new OpenIddictScopeDescriptor();
        AdminOpenIddictManagementSupport.ApplyScopeInput(descriptor, Input, isCreate: true);
        await scopeManager.CreateAsync(descriptor, cancellationToken);

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ScopeCreated",
            new { Input.Name, Input.DisplayName, Source = "AdminUi.Create" }));
        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Scope {Input.Name} criado com sucesso.";
        return RedirectToPage("/Admin/Scopes");
    }
}