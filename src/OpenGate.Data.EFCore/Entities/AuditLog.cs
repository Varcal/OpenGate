namespace OpenGate.Data.EFCore.Entities;

/// <summary>
/// Immutable audit record for security-relevant events
/// (logins, token issuance, password changes, admin actions, etc.).
/// </summary>
public class AuditLog
{
    public long Id { get; set; }

    /// <summary>
    /// FK to <see cref="OpenGateUser"/>. Null for anonymous or
    /// pre-authentication events (e.g. failed login with unknown username).
    /// </summary>
    public string? UserId { get; set; }
    public OpenGateUser? User { get; set; }

    /// <summary>
    /// Structured event type identifier.
    /// Examples: "Login.Success", "Login.Failed", "Token.Issued",
    /// "Password.Changed", "User.Locked".
    /// </summary>
    public string EventType { get; set; } = default!;

    /// <summary>OAuth 2.0 client_id involved in the event, if any.</summary>
    public string? ClientId { get; set; }

    /// <summary>Remote IP address of the request.</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent header value.</summary>
    public string? UserAgent { get; set; }

    /// <summary>Whether the event represents a successful outcome.</summary>
    public bool Succeeded { get; set; }

    /// <summary>
    /// Optional JSON blob with event-specific details
    /// (e.g. failure reason, changed fields, scopes granted).
    /// </summary>
    public string? Details { get; set; }

    /// <summary>UTC timestamp when the event occurred.</summary>
    public DateTimeOffset OccurredAt { get; set; } = DateTimeOffset.UtcNow;
}

