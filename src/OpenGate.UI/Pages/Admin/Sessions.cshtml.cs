using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGate.Admin.Api.Security;
using OpenGate.Data.EFCore;

namespace OpenGate.UI.Pages.Admin;

public sealed class SessionsModel(OpenGateDbContext db) : AdminPageModel
{
    private const int MaxResults = 50;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    [BindProperty(SupportsGet = true)]
    public bool ActiveOnly { get; set; }

    public bool CanManageSessions
        => User.IsInRole(OpenGateAdminRoles.Admin) || User.IsInRole(OpenGateAdminRoles.SuperAdmin);

    public int TotalCount { get; private set; }
    public IReadOnlyList<AdminSessionListItem> Sessions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var query = db.UserSessions
            .AsNoTracking()
            .Include(session => session.User)
            .OrderByDescending(session => session.CreatedAt)
            .AsQueryable();

        if (ActiveOnly)
        {
            query = query.Where(session => session.RevokedAt == null && session.ExpiresAt > now);
        }

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(session =>
                (session.User.Email ?? string.Empty).Contains(search) ||
                (session.ClientId ?? string.Empty).Contains(search) ||
                (session.DeviceInfo ?? string.Empty).Contains(search) ||
                (session.IpAddress ?? string.Empty).Contains(search));
        }

        TotalCount = await query.CountAsync(cancellationToken);
        Sessions = await query.Take(MaxResults)
            .Select(session => new AdminSessionListItem
            {
                Id = session.Id,
                UserEmail = session.User.Email,
                ClientId = session.ClientId,
                IpAddress = session.IpAddress,
                DeviceInfo = session.DeviceInfo,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                RevokedAt = session.RevokedAt,
                IsActive = session.RevokedAt == null && session.ExpiresAt > now
            })
            .ToListAsync(cancellationToken);
    }

    public async Task<IActionResult> OnPostRevokeAsync(Guid id, CancellationToken cancellationToken)
    {
        if (!CanManageSessions)
        {
            return Forbid();
        }

        var session = await db.UserSessions.SingleOrDefaultAsync(candidate => candidate.Id == id, cancellationToken);
        if (session is null)
        {
            ErrorMessage = "Sessão não encontrada para revogação.";
            return RedirectToPage(new { Search, ActiveOnly });
        }

        if (session.RevokedAt is null)
        {
            session.RevokedAt = DateTimeOffset.UtcNow;

            var auditLog = AdminUserManagementSupport.CreateAuditLog(
                HttpContext,
                User,
                "Admin.SessionRevoked",
                new { sessionId = session.Id, targetUserId = session.UserId, session.ClientId, Source = "AdminUi.Revoke" });
            auditLog.ClientId = session.ClientId;
            db.AuditLogs.Add(auditLog);

            await db.SaveChangesAsync(cancellationToken);
        }

        StatusMessage = $"Sessão {session.Id} revogada com sucesso.";
        return RedirectToPage(new { Search, ActiveOnly });
    }
}

public sealed class AdminSessionListItem
{
    public Guid Id { get; init; }
    public string? UserEmail { get; init; }
    public string? ClientId { get; init; }
    public string? IpAddress { get; init; }
    public string? DeviceInfo { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public DateTimeOffset? RevokedAt { get; init; }
    public bool IsActive { get; init; }
}