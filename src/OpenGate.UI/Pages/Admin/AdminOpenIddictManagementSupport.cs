using Microsoft.AspNetCore.Mvc.ModelBinding;
using OpenIddict.Abstractions;
using static OpenIddict.Abstractions.OpenIddictConstants;

namespace OpenGate.UI.Pages.Admin;

internal static class AdminOpenIddictManagementSupport
{
    public static readonly string[] SupportedClientTypes = [ClientTypes.Public, ClientTypes.Confidential];
    public static readonly string[] SupportedConsentTypes =
    [
        ConsentTypes.Explicit,
        ConsentTypes.Implicit,
        ConsentTypes.External,
        ConsentTypes.Systematic
    ];

    public static string NormalizeClientType(string? clientType)
        => string.IsNullOrWhiteSpace(clientType) ? ClientTypes.Public : clientType.Trim();

    public static string NormalizeConsentType(string? consentType)
        => string.IsNullOrWhiteSpace(consentType) ? ConsentTypes.Explicit : consentType.Trim();

    public static string[] ParseList(string? raw)
        => (raw ?? string.Empty)
            .Split([',', ';', '\r', '\n'], StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(value => !string.IsNullOrWhiteSpace(value))
            .Distinct(StringComparer.Ordinal)
            .ToArray();

    public static string ToTextBlock(IEnumerable<string> values)
        => string.Join(Environment.NewLine, values.Where(value => !string.IsNullOrWhiteSpace(value)));

    public static void ValidateClientInput(
        ModelStateDictionary modelState,
        ClientFormInput input,
        bool isCreate,
        string? routeClientId = null)
    {
        var clientType = NormalizeClientType(input.ClientType);
        var consentType = NormalizeConsentType(input.ConsentType);
        var redirectUris = ParseList(input.RedirectUris);
        var postLogoutRedirectUris = ParseList(input.PostLogoutRedirectUris);
        var permissions = ParseList(input.Permissions);

        if (isCreate && string.IsNullOrWhiteSpace(input.ClientId))
        {
            modelState.AddModelError(nameof(input.ClientId), "ClientId é obrigatório.");
        }

        if (!string.IsNullOrWhiteSpace(routeClientId)
            && !string.IsNullOrWhiteSpace(input.ClientId)
            && !string.Equals(routeClientId, input.ClientId, StringComparison.Ordinal))
        {
            modelState.AddModelError(nameof(input.ClientId), "O ClientId não pode ser alterado na edição.");
        }

        if (!SupportedClientTypes.Contains(clientType, StringComparer.Ordinal))
        {
            modelState.AddModelError(nameof(input.ClientType), "Client type inválido.");
        }

        if (!SupportedConsentTypes.Contains(consentType, StringComparer.Ordinal))
        {
            modelState.AddModelError(nameof(input.ConsentType), "Consent type inválido.");
        }

        if (isCreate && permissions.Length == 0)
        {
            modelState.AddModelError(nameof(input.Permissions), "Informe ao menos uma permissão para criar o client.");
        }

        if (string.Equals(clientType, ClientTypes.Confidential, StringComparison.Ordinal)
            && isCreate
            && string.IsNullOrWhiteSpace(input.ClientSecret))
        {
            modelState.AddModelError(nameof(input.ClientSecret), "Client secret é obrigatório para clients confidential.");
        }

        ValidateAbsoluteUris(modelState, nameof(input.RedirectUris), redirectUris, "Todas as redirect URIs devem ser absolutas.");
        ValidateAbsoluteUris(modelState, nameof(input.PostLogoutRedirectUris), postLogoutRedirectUris, "Todas as post logout redirect URIs devem ser absolutas.");
    }

    public static void ApplyClientInput(OpenIddictApplicationDescriptor descriptor, ClientFormInput input, bool isCreate)
    {
        if (isCreate)
        {
            descriptor.ClientId = input.ClientId?.Trim();
        }

        descriptor.DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? null : input.DisplayName.Trim();
        descriptor.ClientType = NormalizeClientType(input.ClientType);
        descriptor.ConsentType = NormalizeConsentType(input.ConsentType);

        if (isCreate || !string.IsNullOrWhiteSpace(input.ClientSecret))
        {
            descriptor.ClientSecret = string.IsNullOrWhiteSpace(input.ClientSecret) ? null : input.ClientSecret.Trim();
        }

        descriptor.RedirectUris.Clear();
        foreach (var uri in ParseList(input.RedirectUris))
        {
            descriptor.RedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        descriptor.PostLogoutRedirectUris.Clear();
        foreach (var uri in ParseList(input.PostLogoutRedirectUris))
        {
            descriptor.PostLogoutRedirectUris.Add(new Uri(uri, UriKind.Absolute));
        }

        descriptor.Permissions.Clear();
        foreach (var permission in ParseList(input.Permissions))
        {
            descriptor.Permissions.Add(permission);
        }

        descriptor.Requirements.Clear();
        foreach (var requirement in ParseList(input.Requirements))
        {
            descriptor.Requirements.Add(requirement);
        }
    }

    public static void ValidateScopeInput(
        ModelStateDictionary modelState,
        ScopeFormInput input,
        bool isCreate,
        string? routeName = null)
    {
        if (isCreate && string.IsNullOrWhiteSpace(input.Name))
        {
            modelState.AddModelError(nameof(input.Name), "O nome do scope é obrigatório.");
        }

        if (!string.IsNullOrWhiteSpace(routeName)
            && !string.IsNullOrWhiteSpace(input.Name)
            && !string.Equals(routeName, input.Name, StringComparison.Ordinal))
        {
            modelState.AddModelError(nameof(input.Name), "O nome do scope não pode ser alterado na edição.");
        }
    }

    public static void ApplyScopeInput(OpenIddictScopeDescriptor descriptor, ScopeFormInput input, bool isCreate)
    {
        if (isCreate)
        {
            descriptor.Name = input.Name?.Trim();
        }

        descriptor.DisplayName = string.IsNullOrWhiteSpace(input.DisplayName) ? null : input.DisplayName.Trim();
        descriptor.Description = string.IsNullOrWhiteSpace(input.Description) ? null : input.Description.Trim();

        descriptor.Resources.Clear();
        foreach (var resource in ParseList(input.Resources))
        {
            descriptor.Resources.Add(resource);
        }
    }

    public static async Task DeleteEntityAsync(
        OpenGate.Data.EFCore.OpenGateDbContext db,
        object entity,
        Func<ValueTask> deleteUsingManager,
        CancellationToken cancellationToken)
    {
        try
        {
            await deleteUsingManager();
        }
        catch (InvalidOperationException exception) when (exception.Message.Contains("ExecuteDelete", StringComparison.Ordinal))
        {
            db.Remove(entity);
            await db.SaveChangesAsync(cancellationToken);
        }
    }

    private static void ValidateAbsoluteUris(
        ModelStateDictionary modelState,
        string key,
        IEnumerable<string> values,
        string message)
    {
        foreach (var value in values)
        {
            if (!Uri.TryCreate(value, UriKind.Absolute, out _))
            {
                modelState.AddModelError(key, message);
                return;
            }
        }
    }
}

public sealed class ClientFormInput
{
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? DisplayName { get; set; }
    public string? ClientType { get; set; }
    public string? ConsentType { get; set; }
    public string? RedirectUris { get; set; }
    public string? PostLogoutRedirectUris { get; set; }
    public string? Permissions { get; set; }
    public string? Requirements { get; set; }
}

public sealed class ScopeFormInput
{
    public string? Name { get; set; }
    public string? DisplayName { get; set; }
    public string? Description { get; set; }
    public string? Resources { get; set; }
}