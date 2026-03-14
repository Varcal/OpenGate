using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGate.Admin.Api.Security;
using OpenGate.Data.EFCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Admin;

public sealed class UsersModel(
    OpenGateDbContext db,
    UserManager<OpenGateUser> userManager,
    IAuthorizationService authorizationService) : AdminPageModel
{
    private const int MaxResults = 50;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public bool CanManageUsers
        => User.IsInRole(OpenGateAdminRoles.Admin) || User.IsInRole(OpenGateAdminRoles.SuperAdmin);

    public int TotalCount { get; private set; }
    public IReadOnlyList<AdminUserListItem> Users { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = db.Users
            .AsNoTracking()
            .Include(user => user.Profile)
            .OrderBy(user => user.Email)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(user =>
                (user.Email ?? string.Empty).Contains(search) ||
                (user.UserName ?? string.Empty).Contains(search) ||
                (user.Profile != null &&
                    (((user.Profile.DisplayName ?? string.Empty).Contains(search)) ||
                     ((user.Profile.FirstName ?? string.Empty).Contains(search)) ||
                     ((user.Profile.LastName ?? string.Empty).Contains(search)))));
        }

        TotalCount = await query.CountAsync(cancellationToken);

        var now = DateTimeOffset.UtcNow;
        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var users = await query.Take(MaxResults).ToListAsync(cancellationToken);
        var items = new List<AdminUserListItem>(users.Count);

        foreach (var user in users)
        {
            var roles = await userManager.GetRolesAsync(user);
            var activeSessionCount = await db.UserSessions.CountAsync(
                session => session.UserId == user.Id && session.RevokedAt == null && session.ExpiresAt > now,
                cancellationToken);

            items.Add(new AdminUserListItem
            {
                Id = user.Id,
                Email = user.Email,
                UserName = user.UserName,
                DisplayName = user.Profile?.DisplayName,
                FirstName = user.Profile?.FirstName,
                LastName = user.Profile?.LastName,
                IsActive = user.IsActive,
                EmailConfirmed = user.EmailConfirmed,
                CreatedAt = user.CreatedAt,
                LastLoginAt = user.LastLoginAt,
                ActiveSessionCount = activeSessionCount,
                IsCurrentUser = string.Equals(user.Id, currentUserId, StringComparison.Ordinal),
                Roles = roles.OrderBy(role => role, StringComparer.Ordinal).ToArray()
            });
        }

        Users = items;
    }

    public async Task<IActionResult> OnPostToggleActiveAsync(string id, bool isActive, CancellationToken cancellationToken)
    {
        var authorization = await authorizationService.AuthorizeAsync(User, resource: null, OpenGateAdminPolicies.Admin);
        if (!authorization.Succeeded)
        {
            return Forbid();
        }

        var user = await userManager.FindByIdAsync(id);
        if (user is null)
        {
            ErrorMessage = "Usuário não encontrado para atualização.";
            return RedirectToPage(new { Search });
        }

        var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.Equals(currentUserId, user.Id, StringComparison.Ordinal) && !isActive)
        {
            ErrorMessage = "Você não pode desativar a própria conta pelo Admin UI.";
            return RedirectToPage(new { Search });
        }

        var previousState = user.IsActive;
        if (previousState == isActive)
        {
            return RedirectToPage(new { Search });
        }

        user.IsActive = isActive;
        var updateResult = await userManager.UpdateAsync(user);
        if (!updateResult.Succeeded)
        {
            ErrorMessage = string.Join(" ", updateResult.Errors.Select(error => error.Description));
            return RedirectToPage(new { Search });
        }

        if (previousState && !isActive)
        {
            await AdminUserManagementSupport.RevokeActiveSessionsAsync(db, user.Id, cancellationToken);
        }

        db.AuditLogs.Add(AdminUserManagementSupport.CreateAuditLog(
            HttpContext,
            User,
            "Admin.UserUpdated",
            new { user.Id, user.Email, user.IsActive, Source = "AdminUi.ToggleActive" }));

        await db.SaveChangesAsync(cancellationToken);

        StatusMessage = isActive
            ? $"Usuário {user.Email} ativado com sucesso."
            : $"Usuário {user.Email} desativado com sucesso.";

        return RedirectToPage(new { Search });
    }
}

public sealed class AdminUserListItem
{
    public string Id { get; init; } = string.Empty;
    public string? Email { get; init; }
    public string? UserName { get; init; }
    public string? DisplayName { get; init; }
    public string? FirstName { get; init; }
    public string? LastName { get; init; }
    public bool IsActive { get; init; }
    public bool EmailConfirmed { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset? LastLoginAt { get; init; }
    public int ActiveSessionCount { get; init; }
    public bool IsCurrentUser { get; init; }
    public IReadOnlyList<string> Roles { get; init; } = [];
}