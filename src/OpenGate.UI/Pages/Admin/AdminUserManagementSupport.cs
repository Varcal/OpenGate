using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.EntityFrameworkCore;
using OpenGate.Admin.Api.Security;
using OpenGate.Data.EFCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.UI.Pages.Admin;

internal static class AdminUserManagementSupport
{
    public static async Task<string[]> GetAvailableRolesAsync(RoleManager<IdentityRole> roleManager, CancellationToken cancellationToken)
        => await roleManager.Roles
            .AsNoTracking()
            .OrderBy(role => role.Name)
            .Select(role => role.Name!)
            .Where(role => role != null)
            .ToArrayAsync(cancellationToken);

    public static string[] NormalizeRoles(IEnumerable<string>? selectedRoles)
        => selectedRoles?
            .Where(role => !string.IsNullOrWhiteSpace(role))
            .Select(role => role.Trim())
            .Distinct(StringComparer.Ordinal)
            .ToArray()
            ?? [];

    public static UserProfile BuildOrUpdateProfile(
        UserProfile? profile,
        string? firstName,
        string? lastName,
        string? displayName,
        string? locale,
        string? timeZone)
    {
        profile ??= new UserProfile();
        profile.FirstName = firstName;
        profile.LastName = lastName;
        profile.DisplayName = displayName;
        profile.Locale = locale;
        profile.TimeZone = timeZone;
        profile.UpdatedAt = DateTimeOffset.UtcNow;
        return profile;
    }

    public static async Task<IdentityResult> ReplaceRolesAsync(
        UserManager<OpenGateUser> userManager,
        OpenGateUser user,
        IEnumerable<string>? selectedRoles)
    {
        var desiredRoles = NormalizeRoles(selectedRoles);
        var currentRoles = await userManager.GetRolesAsync(user);

        var rolesToRemove = currentRoles.Except(desiredRoles, StringComparer.Ordinal).ToArray();
        if (rolesToRemove.Length > 0)
        {
            var removeResult = await userManager.RemoveFromRolesAsync(user, rolesToRemove);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }
        }

        var rolesToAdd = desiredRoles.Except(currentRoles, StringComparer.Ordinal).ToArray();
        return rolesToAdd.Length == 0
            ? IdentityResult.Success
            : await userManager.AddToRolesAsync(user, rolesToAdd);
    }

    public static async Task RevokeActiveSessionsAsync(
        OpenGateDbContext db,
        string userId,
        CancellationToken cancellationToken)
    {
        var sessions = await db.UserSessions
            .Where(session => session.UserId == userId && session.RevokedAt == null && session.ExpiresAt > DateTimeOffset.UtcNow)
            .ToListAsync(cancellationToken);

        if (sessions.Count == 0)
        {
            return;
        }

        var revokedAt = DateTimeOffset.UtcNow;
        foreach (var session in sessions)
        {
            session.RevokedAt = revokedAt;
        }
    }

    public static bool HasWriteRole(IEnumerable<string> roles)
        => roles.Contains(OpenGateAdminRoles.Admin, StringComparer.Ordinal)
            || roles.Contains(OpenGateAdminRoles.SuperAdmin, StringComparer.Ordinal);

    public static AuditLog CreateAuditLog(
        HttpContext httpContext,
        ClaimsPrincipal principal,
        string eventType,
        object details)
        => new()
        {
            UserId = principal.FindFirstValue(ClaimTypes.NameIdentifier),
            EventType = eventType,
            IpAddress = httpContext.Connection.RemoteIpAddress?.ToString(),
            UserAgent = httpContext.Request.Headers["User-Agent"].ToString(),
            Succeeded = true,
            Details = JsonSerializer.Serialize(details),
            OccurredAt = DateTimeOffset.UtcNow
        };

    public static void AddIdentityErrors(ModelStateDictionary modelState, IdentityResult result)
    {
        foreach (var error in result.Errors)
        {
            modelState.AddModelError(string.Empty, error.Description);
        }
    }
}