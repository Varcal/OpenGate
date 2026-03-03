using System.Security.Claims;
using OpenIddict.Abstractions;
using OpenIddict.Server;
using OpenIddict.Server.AspNetCore;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenGate.Server.Internal;

/// <summary>
/// Default token issuance logic for the client_credentials grant.
/// 
/// OpenIddict implements the protocol pipeline but does not automatically issue
/// tokens for client_credentials: a principal must be provided by the host.
/// This handler provides a conservative, turnkey default for machine-to-machine
/// clients.
/// </summary>
internal sealed class ClientCredentialsTokenHandler(IOpenIddictScopeManager scopeManager)
    : IOpenIddictServerHandler<OpenIddictServerEvents.HandleTokenRequestContext>
{
    public async ValueTask HandleAsync(OpenIddictServerEvents.HandleTokenRequestContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        if (!context.Request.IsClientCredentialsGrantType())
            return;

        var clientId = context.Request.ClientId;
        if (string.IsNullOrWhiteSpace(clientId))
            throw new InvalidOperationException("The client_id parameter is missing.");

        var identity = new ClaimsIdentity(
            authenticationType: OpenIddictServerAspNetCoreDefaults.AuthenticationScheme,
            nameType: Claims.Name,
            roleType: Claims.Role);

        identity.AddClaim(new Claim(Claims.Subject, clientId));
        identity.AddClaim(new Claim(Claims.Name, clientId));

        var principal = new ClaimsPrincipal(identity);

        var scopes = context.Request.GetScopes();
        principal.SetScopes(scopes);

        // If scopes are registered in the scope store, attach the corresponding resources
        // so OpenIddict can compute audiences ("aud") appropriately.
        var resources = new HashSet<string>(StringComparer.Ordinal);
        await foreach (var resource in scopeManager.ListResourcesAsync(scopes))
            resources.Add(resource);

        if (resources.Count > 0)
            principal.SetResources(resources);

        principal.SetDestinations(static claim => claim.Type switch
        {
            Claims.Subject or Claims.Name => [Destinations.AccessToken],
            _ => []
        });

        context.SignIn(principal);
    }
}

