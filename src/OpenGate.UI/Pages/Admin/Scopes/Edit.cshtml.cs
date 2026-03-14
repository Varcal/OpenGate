using Microsoft.AspNetCore.Mvc;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin.Scopes;

public sealed class EditModel(
    IOpenIddictScopeManager scopeManager,
    OpenGateDbContext db) : AdminWritePageModel
{
    [BindProperty]
    public ScopeFormInput Input { get; set; } = new();

    public async Task<IActionResult> OnGetAsync(string name, CancellationToken cancellationToken)
    {
        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, scope, cancellationToken);
        Input = new ScopeFormInput
        {
            Name = descriptor.Name,
            DisplayName = descriptor.DisplayName,
            Description = descriptor.Description,
            Resources = AdminOpenIddictManagementSupport.ToTextBlock(descriptor.Resources)
        };

        return Page();
    }

    public async Task<IActionResult> OnPostAsync(string name, CancellationToken cancellationToken)
    {
        AdminOpenIddictManagementSupport.ValidateScopeInput(ModelState, Input, isCreate: false, routeName: name);
        if (!ModelState.IsValid)
        {
            return Page();
        }

        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope is null)
        {
            return NotFound();
        }

        var descriptor = new OpenIddictScopeDescriptor();
        await scopeManager.PopulateAsync(descriptor, scope, cancellationToken);
        AdminOpenIddictManagementSupport.ApplyScopeInput(descriptor, Input, isCreate: false);
        await scopeManager.UpdateAsync(scope, descriptor, cancellationToken);

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ScopeUpdated",
            new { name, Input.DisplayName, Source = "AdminUi.Edit" }));
        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = $"Scope {name} atualizado com sucesso.";
        return RedirectToPage("/Admin/Scopes");
    }
}