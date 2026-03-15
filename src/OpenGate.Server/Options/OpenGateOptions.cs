using Microsoft.EntityFrameworkCore;

namespace OpenGate.Server.Options;

/// <summary>
/// Configuration options for the OpenGate Identity Server.
/// Set via <c>services.AddOpenGate(options => { ... })</c>.
/// </summary>
public sealed class OpenGateOptions
{
    // ── UI ───────────────────────────────────────────────────────────────────

    /// <summary>
    /// Controls whether the host uses the built-in OpenGate UI, a custom UI, or no interactive UI.
    /// Default: <see cref="OpenGateUiMode.BuiltIn"/>.
    /// </summary>
    public OpenGateUiMode UiMode { get; set; } = OpenGateUiMode.BuiltIn;

    /// <summary>
    /// Login path used by the Identity application cookie challenge.
    /// For custom UI scenarios, point this to your own login route.
    /// Default: <c>/Account/Login</c>.
    /// </summary>
    public string LoginPath { get; set; } = "/Account/Login";

    /// <summary>
    /// Access denied path used by the Identity application cookie.
    /// For custom UI scenarios, point this to your own route.
    /// Default: <c>/Account/AccessDenied</c>.
    /// </summary>
    public string AccessDeniedPath { get; set; } = "/Account/AccessDenied";

    // ── Security ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Security preset to apply. Defaults to <see cref="OpenGateSecurityPreset.Production"/>.
    /// Overrides individual token lifetime settings when set.
    /// </summary>
    public OpenGateSecurityPreset SecurityPreset { get; set; } = OpenGateSecurityPreset.Production;

    // ── Issuer ────────────────────────────────────────────────────────────────

    /// <summary>
    /// The issuer URI advertised in the OpenID Connect discovery document.
    /// If null, ASP.NET Core's detected base URI is used.
    /// Example: <c>https://identity.mycompany.com</c>
    /// </summary>
    public Uri? IssuerUri { get; set; }

    // ── Scopes ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Name of the default API scope registered in OpenIddict server metadata.
    /// Used by the sample app/tests for the client_credentials flow.
    /// Default: <c>"api"</c>.
    /// </summary>
    public string ApiScopeName { get; set; } = "api";

    // ── Token Lifetimes ───────────────────────────────────────────────────────

    /// <summary>Lifetime of issued access tokens. Default: 1 hour.</summary>
    public TimeSpan AccessTokenLifetime { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Lifetime of issued refresh tokens. Default: 14 days.
    /// Rotation is always enabled; each use issues a new token.
    /// </summary>
    public TimeSpan RefreshTokenLifetime { get; set; } = TimeSpan.FromDays(14);

    /// <summary>Lifetime of authorization codes. Default: 5 minutes.</summary>
    public TimeSpan AuthorizationCodeLifetime { get; set; } = TimeSpan.FromMinutes(5);

    // ── Endpoints ─────────────────────────────────────────────────────────────

    /// <summary>Path for the authorization endpoint. Default: <c>/connect/authorize</c></summary>
    public string AuthorizationEndpointPath { get; set; } = "/connect/authorize";

    /// <summary>Path for the token endpoint. Default: <c>/connect/token</c></summary>
    public string TokenEndpointPath { get; set; } = "/connect/token";

    /// <summary>Path for the logout/end-session endpoint. Default: <c>/connect/logout</c></summary>
    public string LogoutEndpointPath { get; set; } = "/connect/logout";

    /// <summary>Path for the userinfo endpoint. Default: <c>/connect/userinfo</c></summary>
    public string UserinfoEndpointPath { get; set; } = "/connect/userinfo";

    /// <summary>Path for the introspection endpoint. Default: <c>/connect/introspect</c></summary>
    public string IntrospectionEndpointPath { get; set; } = "/connect/introspect";

    /// <summary>Path for the revocation endpoint. Default: <c>/connect/revoke</c></summary>
    public string RevocationEndpointPath { get; set; } = "/connect/revoke";

    /// <summary>Path for the device authorization endpoint. Default: <c>/connect/device</c></summary>
    public string DeviceEndpointPath { get; set; } = "/connect/device";

    /// <summary>
    /// Path for the device verification endpoint (where users enter the user code).
    /// Default: <c>/connect/verify</c>. Required when <see cref="EnableDeviceFlow"/> is <c>true</c>.
    /// </summary>
    public string DeviceVerificationEndpointPath { get; set; } = "/connect/verify";

    // ── Database ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Action to configure the underlying <see cref="DbContextOptionsBuilder"/>.
    /// Required unless <c>OpenGateDbContext</c> is already registered in DI.
    /// </summary>
    public Action<DbContextOptionsBuilder>? ConfigureDatabase { get; set; }

    // ── Features ──────────────────────────────────────────────────────────────

    /// <summary>
    /// Whether to enable the Device Authorization flow (RFC 8628).
    /// Default: <c>true</c>.
    /// </summary>
    public bool EnableDeviceFlow { get; set; } = true;

    /// <summary>
    /// Whether to enable token introspection (RFC 7662).
    /// Default: <c>true</c>.
    /// </summary>
    public bool EnableIntrospection { get; set; } = true;

    /// <summary>
    /// Whether to enable token revocation (RFC 7009).
    /// Default: <c>true</c>.
    /// </summary>
    public bool EnableRevocation { get; set; } = true;
}
