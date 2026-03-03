namespace OpenGate.Data.EFCore.Entities;

/// <summary>
/// Tracks active user sessions. Enables session listing, device management
/// and remote revocation from the Account Management UI.
/// </summary>
public class UserSession
{
    public Guid Id { get; set; }

    /// <summary>FK to <see cref="OpenGateUser"/>.</summary>
    public string UserId { get; set; } = default!;
    public OpenGateUser User { get; set; } = default!;

    /// <summary>OAuth 2.0 client_id that initiated the session.</summary>
    public string? ClientId { get; set; }

    /// <summary>Remote IP address at session creation.</summary>
    public string? IpAddress { get; set; }

    /// <summary>User-Agent header at session creation.</summary>
    public string? UserAgent { get; set; }

    /// <summary>
    /// Human-readable device description derived from the User-Agent
    /// (e.g. "Chrome 122 on Windows 11").
    /// </summary>
    public string? DeviceInfo { get; set; }

    /// <summary>UTC timestamp when the session was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp when the session expires.</summary>
    public DateTimeOffset ExpiresAt { get; set; }

    /// <summary>UTC timestamp when the session was explicitly revoked. Null = not revoked.</summary>
    public DateTimeOffset? RevokedAt { get; set; }

    /// <summary>
    /// Whether the session is currently valid.
    /// A session is active when it has not been revoked and has not expired.
    /// </summary>
    public bool IsActive => RevokedAt is null && ExpiresAt > DateTimeOffset.UtcNow;
}

