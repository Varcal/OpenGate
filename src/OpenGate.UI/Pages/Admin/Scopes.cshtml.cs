using Microsoft.AspNetCore.Mvc;
using OpenGate.Admin.Api.Security;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin;

public sealed class ScopesModel(
    IOpenIddictScopeManager scopeManager,
    OpenGateDbContext db) : AdminPageModel
{
    private const int MaxResults = 50;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public bool CanManageScopes
        => User.IsInRole(OpenGateAdminRoles.Admin) || User.IsInRole(OpenGateAdminRoles.SuperAdmin);

    public int TotalCount { get; private set; }
    public IReadOnlyList<AdminScopeListItem> Scopes { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = new List<AdminScopeListItem>();

        await foreach (var scope in scopeManager.ListAsync(null, null, cancellationToken))
        {
            var descriptor = new OpenIddictScopeDescriptor();
            await scopeManager.PopulateAsync(descriptor, scope, cancellationToken);

            if (!MatchesSearch(descriptor, Search))
            {
                continue;
            }

            TotalCount++;
            if (items.Count >= MaxResults)
            {
                continue;
            }

            items.Add(new AdminScopeListItem
            {
                Name = descriptor.Name,
                DisplayName = descriptor.DisplayName,
                Description = descriptor.Description,
                ResourceCount = descriptor.Resources.Count
            });
        }

        Scopes = items;
    }

    private static bool MatchesSearch(OpenIddictScopeDescriptor descriptor, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        return (descriptor.Name?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || (descriptor.DisplayName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || (descriptor.Description?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || descriptor.Resources.Any(resource => resource.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IActionResult> OnPostDeleteAsync(string name, CancellationToken cancellationToken)
    {
        if (!CanManageScopes)
        {
            return Forbid();
        }

        var scope = await scopeManager.FindByNameAsync(name, cancellationToken);
        if (scope is null)
        {
            ErrorMessage = "Scope não encontrado para exclusão.";
            return RedirectToPage(new { Search });
        }

        await AdminOpenIddictManagementSupport.DeleteEntityAsync(
            db,
            scope,
            () => scopeManager.DeleteAsync(scope, cancellationToken),
            cancellationToken);
        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ScopeDeleted",
            new { name, Source = "AdminUi.Delete" }));
        await db.SaveChangesAsync(cancellationToken);
        StatusMessage = $"Scope {name} removido com sucesso.";
        return RedirectToPage(new { Search });
    }
}

public sealed class AdminScopeListItem
{
    public string? Name { get; init; }
    public string? DisplayName { get; init; }
    public string? Description { get; init; }
    public int ResourceCount { get; init; }
}