using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenGate.Server.Tests.Integration;

/// <summary>
/// Integration tests for the OAuth 2.0 token endpoint (/connect/token).
/// </summary>
public sealed class TokenEndpointTests(OpenGateWebFactory factory)
    : IClassFixture<OpenGateWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient();

    private static FormUrlEncodedContent ClientCredentialsForm(
        string clientId,
        string clientSecret,
        string scope = "api") => new(new Dictionary<string, string>
    {
        ["grant_type"]    = "client_credentials",
        ["client_id"]     = clientId,
        ["client_secret"] = clientSecret,
        ["scope"]         = scope
    });

    // ── Successful client_credentials ─────────────────────────────────────────

    [Fact]
    public async Task Token_ClientCredentials_Returns_200()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                IntegrationSeedService.MachineClientSecret));

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Token_ClientCredentials_Returns_AccessToken()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                IntegrationSeedService.MachineClientSecret));

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        Assert.True(json.RootElement.TryGetProperty("access_token", out var token));
        Assert.NotEmpty(token.GetString() ?? string.Empty);
    }

    [Fact]
    public async Task Token_ClientCredentials_TokenType_Is_Bearer()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                IntegrationSeedService.MachineClientSecret));

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        Assert.Equal("Bearer",
            json.RootElement.GetProperty("token_type").GetString(),
            StringComparer.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Token_ClientCredentials_ExpiresIn_Is_Positive()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                IntegrationSeedService.MachineClientSecret));

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);

        if (json.RootElement.TryGetProperty("expires_in", out var expiresIn))
            Assert.True(expiresIn.GetInt64() > 0);
    }

    // ── Error scenarios ───────────────────────────────────────────────────────

    [Fact]
    public async Task Token_WrongSecret_Returns_401()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                "totally-wrong-secret"));

        // OpenIddict returns 401 for invalid_client per OAuth 2.0 recommendations.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_UnknownClient_Returns_401()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm("non-existent-client", "any-secret"));

        // OpenIddict returns 401 for invalid_client per OAuth 2.0 recommendations.
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Token_InvalidGrantType_Returns_400()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "invalid_grant_type"
            }));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_MissingBody_Returns_400()
    {
        var request = new HttpRequestMessage(HttpMethod.Post, "/connect/token");
        request.Content = new StringContent(string.Empty);
        request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

        var response = await _client.SendAsync(request);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Token_Error_Response_Contains_Error_Field()
    {
        var response = await _client.PostAsync(
            "/connect/token",
            ClientCredentialsForm(
                IntegrationSeedService.MachineClientId,
                "wrong-secret"));

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        Assert.True(json.RootElement.TryGetProperty("error", out var error));
        Assert.NotEmpty(error.GetString() ?? string.Empty);
    }
}

