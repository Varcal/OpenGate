using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace OpenGate.Server.Tests.Integration;

/// <summary>
/// Integration tests for the OpenID Connect discovery endpoints and health check.
/// </summary>
public sealed class DiscoveryAndHealthTests(OpenGateWebFactory factory)
    : IClassFixture<OpenGateWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    // ── Health ────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Health_Returns_200_With_Status_Healthy()
    {
        var response = await _client.GetAsync("/health");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        Assert.Equal("healthy", json.RootElement.GetProperty("status").GetString());
    }

    // ── Discovery document ────────────────────────────────────────────────────

    [Fact]
    public async Task Discovery_Returns_200_With_ApplicationJson()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("application/json",
            response.Content.Headers.ContentType?.MediaType);
    }

    [Theory]
    [InlineData("issuer")]
    [InlineData("authorization_endpoint")]
    [InlineData("token_endpoint")]
    [InlineData("userinfo_endpoint")]
    [InlineData("end_session_endpoint")]
    [InlineData("jwks_uri")]
    [InlineData("response_types_supported")]
    [InlineData("grant_types_supported")]
    [InlineData("scopes_supported")]
    public async Task Discovery_Contains_Required_Field(string field)
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.NotNull(json);
        Assert.True(json.RootElement.TryGetProperty(field, out _),
            $"Discovery document is missing required field '{field}'.");
    }

    [Fact]
    public async Task Discovery_Issuer_Matches_Configuration()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.NotNull(json);
        // OpenIddict may normalize the URI with a trailing slash; strip it for comparison.
        var issuer = json.RootElement.GetProperty("issuer").GetString()?.TrimEnd('/');
        Assert.Equal("https://localhost", issuer);
    }

    [Fact]
    public async Task Discovery_GrantTypes_Includes_Expected_Flows()
    {
        var response = await _client.GetAsync("/.well-known/openid-configuration");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.NotNull(json);
        var grants = json.RootElement
            .GetProperty("grant_types_supported")
            .EnumerateArray()
            .Select(e => e.GetString())
            .ToHashSet();

        Assert.Contains("authorization_code",  grants);
        Assert.Contains("client_credentials",  grants);
        Assert.Contains("refresh_token",        grants);
    }

    // ── JWKS ──────────────────────────────────────────────────────────────────

    [Fact]
    public async Task Jwks_Returns_200_With_Keys_Array()
    {
        // Retrieve the JWKS URI from discovery
        var discovery = await _client.GetAsync("/.well-known/openid-configuration");
        var discoveryJson = await discovery.Content.ReadFromJsonAsync<JsonDocument>();
        var jwksUri = discoveryJson!.RootElement.GetProperty("jwks_uri").GetString()!;

        var response = await _client.GetAsync(jwksUri);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.NotNull(json);
        Assert.True(json.RootElement.TryGetProperty("keys", out var keys));
        Assert.True(keys.GetArrayLength() > 0, "JWKS must contain at least one signing key.");
    }
}

