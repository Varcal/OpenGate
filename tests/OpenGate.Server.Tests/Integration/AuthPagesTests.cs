using System.Net;

namespace OpenGate.Server.Tests.Integration;

/// <summary>
/// Integration tests for authentication Razor Pages and OAuth flow redirects.
/// </summary>
public sealed class AuthPagesTests(OpenGateWebFactory factory)
    : IClassFixture<OpenGateWebFactory>
{
    // AllowAutoRedirect = false so we can assert on 3xx responses directly
    private readonly HttpClient _client = factory.CreateClient(
        new() { AllowAutoRedirect = false });

    // ── Login page ─────────────────────────────────────────────────────────────

    [Fact]
    public async Task Login_Get_Returns_200()
    {
        var response = await _client.GetAsync("/Account/Login");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_Get_Returns_Html()
    {
        var response = await _client.GetAsync("/Account/Login");
        var content  = await response.Content.ReadAsStringAsync();

        Assert.Equal("text/html", response.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<form", content, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Login_Get_Contains_Email_And_Password_Fields()
    {
        var response = await _client.GetAsync("/Account/Login");
        var content  = await response.Content.ReadAsStringAsync();

        Assert.Contains("type=\"email\"",    content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("type=\"password\"", content, StringComparison.OrdinalIgnoreCase);
    }

    // ── Register page ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Register_Get_Returns_200()
    {
        var response = await _client.GetAsync("/Account/Register");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_Get_Contains_All_Required_Fields()
    {
        var response = await _client.GetAsync("/Account/Register");
        var content  = await response.Content.ReadAsStringAsync();

        // First/Last name, email, password, confirm password
        Assert.Contains("FirstName",        content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("LastName",         content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("type=\"email\"",   content, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("type=\"password\"", content, StringComparison.OrdinalIgnoreCase);
    }

    // ── Root redirect ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Root_Redirects_To_Login()
    {
        var response = await _client.GetAsync("/");

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
        Assert.EndsWith("/Account/Login",
            response.Headers.Location?.ToString() ?? string.Empty);
    }

    // ── Authorization endpoint ─────────────────────────────────────────────────

    [Fact]
    public async Task Authorize_Unauthenticated_Redirects_To_Login()
    {
        // Minimal valid authorization request for the interactive client
        const string url = "/connect/authorize" +
            "?response_type=code" +
            "&client_id=interactive-demo" +
            "&redirect_uri=http%3A%2F%2Flocalhost%2Fcallback" +
            "&scope=openid%20email" +
            "&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" +
            "&code_challenge_method=S256";

        var response = await _client.GetAsync(url);

        // OpenIddict should redirect to the login page
        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);

        var location = response.Headers.Location?.ToString() ?? string.Empty;
        Assert.Contains("Login", location, StringComparison.OrdinalIgnoreCase);
    }

    // ── Logout page ────────────────────────────────────────────────────────────

    [Fact]
    public async Task Logout_Get_Unauthenticated_Redirects_Or_Returns_200()
    {
        var response = await _client.GetAsync("/connect/logout");

        // Unauthenticated logout with no id_token_hint:
        //  • 200 / 3xx — page renders or redirects (authenticated-path fallback)
        //  • 400        — OpenIddict rejects the request as missing required token hint
        // All are acceptable; we just confirm the server is alive and routing works.
        Assert.True(
            response.StatusCode is HttpStatusCode.OK or
                HttpStatusCode.Redirect or
                HttpStatusCode.Found or
                HttpStatusCode.BadRequest,
            $"Unexpected status: {response.StatusCode}");
    }

    // ── CSS static asset ───────────────────────────────────────────────────────

    [Fact]
    public async Task StaticAsset_OpenGateCss_Is_Served()
    {
        var response = await _client.GetAsync("/_content/OpenGate.UI/css/opengate.css");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Contains("text/css",
            response.Content.Headers.ContentType?.MediaType ?? string.Empty,
            StringComparison.OrdinalIgnoreCase);
    }
}

