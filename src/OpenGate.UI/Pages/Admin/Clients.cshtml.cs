using Microsoft.AspNetCore.Mvc;
using OpenGate.Admin.Api.Security;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin;

public sealed class ClientsModel(
    IOpenIddictApplicationManager applicationManager,
    OpenGateDbContext db) : AdminPageModel
{
    private const int MaxResults = 50;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public bool CanManageClients
        => User.IsInRole(OpenGateAdminRoles.Admin) || User.IsInRole(OpenGateAdminRoles.SuperAdmin);

    public int TotalCount { get; private set; }
    public IReadOnlyList<AdminClientListItem> Clients { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var items = new List<AdminClientListItem>();

        await foreach (var application in applicationManager.ListAsync(null, null, cancellationToken))
        {
            var descriptor = new OpenIddictApplicationDescriptor();
            await applicationManager.PopulateAsync(descriptor, application, cancellationToken);

            if (!MatchesSearch(descriptor, Search))
            {
                continue;
            }

            TotalCount++;
            if (items.Count >= MaxResults)
            {
                continue;
            }

            items.Add(new AdminClientListItem
            {
                ClientId = descriptor.ClientId,
                DisplayName = descriptor.DisplayName,
                ClientType = descriptor.ClientType,
                ConsentType = descriptor.ConsentType,
                RedirectUriCount = descriptor.RedirectUris.Count,
                PermissionCount = descriptor.Permissions.Count,
                RequirementCount = descriptor.Requirements.Count
            });
        }

        Clients = items;
    }

    private static bool MatchesSearch(OpenIddictApplicationDescriptor descriptor, string? search)
    {
        if (string.IsNullOrWhiteSpace(search))
        {
            return true;
        }

        var term = search.Trim();
        return (descriptor.ClientId?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || (descriptor.DisplayName?.Contains(term, StringComparison.OrdinalIgnoreCase) ?? false)
            || descriptor.RedirectUris.Any(uri => uri.AbsoluteUri.Contains(term, StringComparison.OrdinalIgnoreCase));
    }

    public async Task<IActionResult> OnPostDeleteAsync(string clientId, CancellationToken cancellationToken)
    {
        if (!CanManageClients)
        {
            return Forbid();
        }

        var application = await applicationManager.FindByClientIdAsync(clientId, cancellationToken);
        if (application is null)
        {
            ErrorMessage = "Client não encontrado para exclusão.";
            return RedirectToPage(new { Search });
        }

        await AdminOpenIddictManagementSupport.DeleteEntityAsync(
            db,
            application,
            () => applicationManager.DeleteAsync(application, cancellationToken),
            cancellationToken);
        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.ClientDeleted",
            new { clientId, Source = "AdminUi.Delete" }));
        await db.SaveChangesAsync(cancellationToken);
        StatusMessage = $"Client {clientId} removido com sucesso.";
        return RedirectToPage(new { Search });
    }
}

public sealed class AdminClientListItem
{
    public string? ClientId { get; init; }
    public string? DisplayName { get; init; }
    public string? ClientType { get; init; }
    public string? ConsentType { get; init; }
    public int RedirectUriCount { get; init; }
    public int PermissionCount { get; init; }
    public int RequirementCount { get; init; }
}