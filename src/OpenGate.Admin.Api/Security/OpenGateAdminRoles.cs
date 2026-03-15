namespace OpenGate.Admin.Api.Security;

/// <summary>
/// Well-known administrative roles used by the OpenGate Admin API.
/// </summary>
public static class OpenGateAdminRoles
{
    public const string SuperAdmin = "SuperAdmin";
    public const string Admin = "Admin";
    public const string Viewer = "Viewer";

    public static readonly string[] All = [SuperAdmin, Admin, Viewer];
}

/// <summary>
/// Well-known authorization policies used by the OpenGate Admin API.
/// </summary>
public static class OpenGateAdminPolicies
{
    public const string Viewer = "OpenGate.Admin.Viewer";
    public const string Admin = "OpenGate.Admin.Admin";
    public const string SuperAdmin = "OpenGate.Admin.SuperAdmin";
    public const string ApiViewer = "OpenGate.Admin.Api.Viewer";
    public const string ApiAdmin = "OpenGate.Admin.Api.Admin";
    public const string ApiSuperAdmin = "OpenGate.Admin.Api.SuperAdmin";
}

/// <summary>
/// Well-known scopes used for headless access to the OpenGate Admin API.
/// </summary>
public static class OpenGateAdminScopes
{
    public const string Read = "admin_api";
    public const string Write = "admin_api.write";

    public static readonly string[] All = [Read, Write];
}
