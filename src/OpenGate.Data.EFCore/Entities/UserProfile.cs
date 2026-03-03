namespace OpenGate.Data.EFCore.Entities;

/// <summary>
/// Extended profile information for an OpenGate user.
/// Stored separately from identity data to keep concerns isolated.
/// </summary>
public class UserProfile
{
    public Guid Id { get; set; }

    /// <summary>FK to <see cref="OpenGateUser"/>.</summary>
    public string UserId { get; set; } = default!;
    public OpenGateUser User { get; set; } = default!;

    public string? FirstName { get; set; }
    public string? LastName { get; set; }

    /// <summary>Full display name. Falls back to username when null.</summary>
    public string? DisplayName { get; set; }

    /// <summary>URL of the user's avatar image.</summary>
    public string? AvatarUrl { get; set; }

    /// <summary>IETF language tag, e.g. "pt-BR", "en-US".</summary>
    public string? Locale { get; set; }

    /// <summary>IANA time zone id, e.g. "America/Sao_Paulo".</summary>
    public string? TimeZone { get; set; }

    /// <summary>UTC timestamp of the last profile update.</summary>
    public DateTimeOffset UpdatedAt { get; set; } = DateTimeOffset.UtcNow;
}

