using OpenGate.Server.Options;

namespace OpenGate.Sample.Basic;

internal static class ProgramUiMode
{
    public static readonly string[] BackendOnlyEndpoints =
    [
        "/.well-known/openid-configuration",
        "/connect/token",
        "/admin/api",
        "/health"
    ];

    public static OpenGateUiMode ParseUiMode(string? raw)
        => Enum.TryParse<OpenGateUiMode>(raw, ignoreCase: true, out var uiMode)
            ? uiMode
            : OpenGateUiMode.BuiltIn;
}
