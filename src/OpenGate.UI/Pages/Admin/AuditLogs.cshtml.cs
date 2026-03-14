using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OpenGate.Data.EFCore;

namespace OpenGate.UI.Pages.Admin;

public sealed class AuditLogsModel(OpenGateDbContext db) : AdminPageModel
{
    private const int MaxResults = 50;

    [BindProperty(SupportsGet = true)]
    public string? Search { get; set; }

    public int TotalCount { get; private set; }
    public IReadOnlyList<AdminAuditLogListItem> AuditLogs { get; private set; } = [];

    public async Task OnGetAsync(CancellationToken cancellationToken)
    {
        var query = db.AuditLogs
            .AsNoTracking()
            .Include(audit => audit.User)
            .OrderByDescending(audit => audit.OccurredAt)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(Search))
        {
            var search = Search.Trim();
            query = query.Where(audit =>
                audit.EventType.Contains(search) ||
                (audit.ClientId ?? string.Empty).Contains(search) ||
                (audit.User != null && (audit.User.Email ?? string.Empty).Contains(search)) ||
                (audit.IpAddress ?? string.Empty).Contains(search));
        }

        TotalCount = await query.CountAsync(cancellationToken);
        AuditLogs = await query.Take(MaxResults)
            .Select(audit => new AdminAuditLogListItem
            {
                Id = audit.Id,
                EventType = audit.EventType,
                UserEmail = audit.User != null ? audit.User.Email : null,
                ClientId = audit.ClientId,
                IpAddress = audit.IpAddress,
                Succeeded = audit.Succeeded,
                Details = audit.Details,
                OccurredAt = audit.OccurredAt
            })
            .ToListAsync(cancellationToken);
    }
}

public sealed class AdminAuditLogListItem
{
    public long Id { get; init; }
    public string EventType { get; init; } = string.Empty;
    public string? UserEmail { get; init; }
    public string? ClientId { get; init; }
    public string? IpAddress { get; init; }
    public bool Succeeded { get; init; }
    public string? Details { get; init; }
    public DateTimeOffset OccurredAt { get; init; }
}