using Microsoft.EntityFrameworkCore;
using OpenGate.Data.EFCore;
using OpenIddict.Abstractions;

namespace OpenGate.UI.Pages.Admin;

public sealed class IndexModel(
    OpenGateDbContext db,
    IOpenIddictApplicationManager applicationManager,
    IOpenIddictScopeManager scopeManager) : AdminPageModel
{
    public AdminDashboardMetrics Metrics { get; private set; } = new();
    public IReadOnlyList<AdminRecentAuditLogItem> RecentAuditLogs { get; private set; } = [];
    public IReadOnlyList<AdminRecentSessionItem> RecentSessions { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var usersTotal = await db.Users.CountAsync(cancellationToken);
        var activeUsers = await db.Users.CountAsync(user => user.IsActive, cancellationToken);
        var activeSessions = await db.UserSessions.CountAsync(
            session => session.RevokedAt == null && session.ExpiresAt > now,
            cancellationToken);
        var auditEventsLast24h = await db.AuditLogs.CountAsync(
            audit => audit.OccurredAt >= now.AddHours(-24),
            cancellationToken);

        var clientsTotal = 0;
        await foreach (var _ in applicationManager.ListAsync(null, null, cancellationToken))
        {
            clientsTotal++;
        }

        var scopesTotal = 0;
        await foreach (var _ in scopeManager.ListAsync(null, null, cancellationToken))
        {
            scopesTotal++;
        }

        Metrics = new AdminDashboardMetrics
        {
            TotalUsers = usersTotal,
            ActiveUsers = activeUsers,
            ActiveSessions = activeSessions,
            AuditEventsLast24h = auditEventsLast24h,
            ClientsTotal = clientsTotal,
            ScopesTotal = scopesTotal,
            GeneratedAt = now
        };

        RecentAuditLogs = await db.AuditLogs
            .AsNoTracking()
            .Include(audit => audit.User)
            .OrderByDescending(audit => audit.OccurredAt)
            .Take(6)
            .Select(audit => new AdminRecentAuditLogItem
            {
                Id = audit.Id,
                EventType = audit.EventType,
                UserEmail = audit.User != null ? audit.User.Email : null,
                ClientId = audit.ClientId,
                Succeeded = audit.Succeeded,
                OccurredAt = audit.OccurredAt
            })
            .ToListAsync(cancellationToken);

        RecentSessions = await db.UserSessions
            .AsNoTracking()
            .Include(session => session.User)
            .OrderByDescending(session => session.CreatedAt)
            .Take(6)
            .Select(session => new AdminRecentSessionItem
            {
                Id = session.Id,
                UserEmail = session.User.Email,
                ClientId = session.ClientId,
                DeviceInfo = session.DeviceInfo,
                CreatedAt = session.CreatedAt,
                ExpiresAt = session.ExpiresAt,
                IsActive = session.RevokedAt == null && session.ExpiresAt > now
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class AdminDashboardMetrics
{
    public int TotalUsers { get; init; }
    public int ActiveUsers { get; init; }
    public int ActiveSessions { get; init; }
    public int AuditEventsLast24h { get; init; }
    public int ClientsTotal { get; init; }
    public int ScopesTotal { get; init; }
    public DateTimeOffset GeneratedAt { get; init; }
}

public sealed class AdminRecentAuditLogItem
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? UserEmail { get; init; }
    public string? ClientId { get; init; }
    public bool Succeeded { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}

public sealed class AdminRecentSessionItem
{
    public Guid Id { get; init; }
    public string? UserEmail { get; init; }
    public string? ClientId { get; init; }
    public string? DeviceInfo { get; init; }
    public DateTimeOffset CreatedAt { get; init; }
    public DateTimeOffset ExpiresAt { get; init; }
    public bool IsActive { get; init; }
}