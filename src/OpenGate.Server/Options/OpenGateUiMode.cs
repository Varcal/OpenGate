namespace OpenGate.Server.Options;

/// <summary>
/// Defines how the host application provides interactive UI for OpenGate.
/// </summary>
public enum OpenGateUiMode
{
    /// <summary>
    /// Uses the built-in OpenGate UI pages and default routes.
    /// </summary>
    BuiltIn = 0,

    /// <summary>
    /// Uses host-provided/custom UI routes for login and access denied flows.
    /// </summary>
    External = 1,

    /// <summary>
    /// Runs without interactive UI. Suitable for protocol/API-only deployments.
    /// </summary>
    None = 2
}
