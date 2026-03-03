using Microsoft.AspNetCore.Identity;

namespace OpenGate.Data.EFCore.Entities;

/// <summary>
/// OpenGate application user. Extends ASP.NET Core Identity's IdentityUser
/// with additional fields for audit and lifecycle management.
/// </summary>
public class OpenGateUser : IdentityUser
{
    /// <summary>UTC timestamp when the user account was created.</summary>
    public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

    /// <summary>UTC timestamp of the user's last successful login.</summary>
    public DateTimeOffset? LastLoginAt { get; set; }

    /// <summary>Whether the account is active. Inactive users cannot authenticate.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public UserProfile? Profile { get; set; }
    public ICollection<AuditLog> AuditLogs { get; set; } = [];
    public ICollection<UserSession> Sessions { get; set; } = [];
}

