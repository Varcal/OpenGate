namespace OpenGate.Server.Options;

/// <summary>
/// Predefined security profiles for <see cref="OpenGateOptions"/>.
/// Choose the profile that matches your deployment environment.
/// </summary>
public enum OpenGateSecurityPreset
{
    /// <summary>
    /// Relaxed settings for local development.
    /// <list type="bullet">
    ///   <item>Development signing credentials (ephemeral RSA key)</item>
    ///   <item>HTTPS not enforced</item>
    ///   <item>Extended token lifetimes (easier debugging)</item>
    ///   <item>Detailed error responses</item>
    /// </list>
    /// <para><b>Never use in production.</b></para>
    /// </summary>
    Development,

    /// <summary>
    /// Secure defaults for production deployments.
    /// <list type="bullet">
    ///   <item>PKCE required for all authorization code flows</item>
    ///   <item>HTTPS enforced</item>
    ///   <item>Refresh token rotation enabled</item>
    ///   <item>Short-lived access tokens (1 hour)</item>
    ///   <item>Opaque error responses</item>
    /// </list>
    /// </summary>
    Production,

    /// <summary>
    /// Stricter settings for high-security deployments (FAPI, banking, healthcare).
    /// <list type="bullet">
    ///   <item>All <see cref="Production"/> settings, plus:</item>
    ///   <item>DPoP required for token binding</item>
    ///   <item>Access tokens expire in 15 minutes</item>
    ///   <item>Refresh tokens expire in 24 hours (no sliding)</item>
    ///   <item>Reference tokens (introspection required by resource servers)</item>
    ///   <item>Mutual TLS (mTLS) support enabled</item>
    /// </list>
    /// </summary>
    HighSecurity,
}

