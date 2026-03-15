using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using OpenGate.Admin.Api.Security;

namespace OpenGate.Server.Tests.Integration;

public sealed class AdminApiTests(OpenGateWebFactory factory)
    : IClassFixture<OpenGateWebFactory>
{
    private readonly HttpClient _client = factory.CreateClient(new() { AllowAutoRedirect = false });
    private static readonly string[] AdminApiResource = ["opengate_admin_api"];

    [Fact]
    public async Task AdminApi_Unauthenticated_Returns_Unauthorized()
    {
        var response = await _client.GetAsync("/admin/api/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task AdminApi_Me_Returns_Current_Admin_User()
    {
        await LoginAsAdminAsync();

        var response = await _client.GetAsync("/admin/api/me");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(json);
        Assert.Equal(IntegrationSeedService.DemoEmail, json.RootElement.GetProperty("email").GetString());
        Assert.Contains(
            json.RootElement.GetProperty("roles").EnumerateArray().Select(x => x.GetString()),
            role => string.Equals(role, "SuperAdmin", StringComparison.Ordinal));
        Assert.Equal("user", json.RootElement.GetProperty("kind").GetString());
    }

    [Fact]
    public async Task AdminApi_Users_Returns_Demo_User()
    {
        await LoginAsAdminAsync();

        var response = await _client.GetAsync("/admin/api/users");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(json);

        var items = json.RootElement.GetProperty("items").EnumerateArray().ToList();
        Assert.Contains(items, item =>
            string.Equals(item.GetProperty("email").GetString(), IntegrationSeedService.DemoEmail, StringComparison.OrdinalIgnoreCase));
    }

    [Fact]
    public async Task AdminApi_BearerReadToken_Can_List_Administrative_Data()
    {
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await RequestAdminTokenAsync(OpenGateAdminScopes.Read));

        var meResponse = await client.GetAsync("/admin/api/me");
        var meJson = await meResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, meResponse.StatusCode);
        Assert.NotNull(meJson);
        Assert.Equal("client", meJson.RootElement.GetProperty("kind").GetString());
        Assert.Equal(IntegrationSeedService.AdminClientId, meJson.RootElement.GetProperty("clientId").GetString());
        Assert.Contains(
            meJson.RootElement.GetProperty("scopes").EnumerateArray().Select(item => item.GetString()),
            scope => scope == OpenGateAdminScopes.Read);

        var clientsResponse = await client.GetAsync("/admin/api/clients");
        var clientsJson = await clientsResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, clientsResponse.StatusCode);
        Assert.NotNull(clientsJson);
        Assert.True(clientsJson.RootElement.GetProperty("count").GetInt32() >= 1);
    }

    [Fact]
    public async Task AdminApi_BearerReadToken_Cannot_Modify_Data()
    {
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await RequestAdminTokenAsync(OpenGateAdminScopes.Read));

        var response = await client.PostAsJsonAsync("/admin/api/scopes", new
        {
            name = "forbidden-scope",
            displayName = "Forbidden Scope",
            resources = AdminApiResource
        });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AdminApi_BearerWriteToken_Can_Create_And_Delete_Scope()
    {
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer",
            await RequestAdminTokenAsync(OpenGateAdminScopes.Write));

        var createResponse = await client.PostAsJsonAsync("/admin/api/scopes", new
        {
            name = "headless-admin-scope",
            displayName = "Headless Admin Scope",
            description = "Created through bearer token automation",
            resources = AdminApiResource
        });

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync("/admin/api/scopes/headless-admin-scope");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task AdminApi_Session_Revoke_Updates_Session_State()
    {
        await LoginAsAdminAsync();

        var sessionsBefore = await _client.GetFromJsonAsync<JsonDocument>("/admin/api/sessions?activeOnly=true");
        Assert.NotNull(sessionsBefore);

        var firstSession = sessionsBefore.RootElement.GetProperty("items").EnumerateArray().First();
        var sessionId = firstSession.GetProperty("id").GetGuid();

        var revokeResponse = await _client.PostAsync($"/admin/api/sessions/{sessionId}/revoke", content: null);
        var revokeJson = await revokeResponse.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(HttpStatusCode.OK, revokeResponse.StatusCode);
        Assert.NotNull(revokeJson);
        Assert.Equal(sessionId, revokeJson.RootElement.GetProperty("id").GetGuid());
        Assert.True(revokeJson.RootElement.GetProperty("revokedAt").ValueKind is not JsonValueKind.Null);

        var sessionsAfter = await _client.GetFromJsonAsync<JsonDocument>("/admin/api/sessions");
        Assert.NotNull(sessionsAfter);

        var revokedSession = sessionsAfter.RootElement.GetProperty("items")
            .EnumerateArray()
            .Single(item => item.GetProperty("id").GetGuid() == sessionId);

        Assert.False(revokedSession.GetProperty("isActive").GetBoolean());
        Assert.True(revokedSession.GetProperty("revokedAt").ValueKind is not JsonValueKind.Null);
    }

    private async Task LoginAsAdminAsync()
    {
        var loginPage = await _client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);

        var antiforgeryToken = ExtractInputValue(html, "__RequestVerificationToken");
        var returnUrl = ExtractInputValue(html, "ReturnUrl");

        var response = await _client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = IntegrationSeedService.DemoEmail,
            ["Input.Password"] = IntegrationSeedService.DemoPassword,
            ["Input.RememberMe"] = "false",
            ["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl,
            ["__RequestVerificationToken"] = antiforgeryToken
        }));

        Assert.Equal(HttpStatusCode.Redirect, response.StatusCode);
    }

    private async Task<string> RequestAdminTokenAsync(string scope)
    {
        using var tokenClient = factory.CreateClient(new() { AllowAutoRedirect = false });

        var response = await tokenClient.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "client_credentials",
            ["client_id"] = IntegrationSeedService.AdminClientId,
            ["client_secret"] = IntegrationSeedService.AdminClientSecret,
            ["scope"] = scope
        }));

        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(json);

        return json.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("The token response did not contain an access token.");
    }

    private static string ExtractInputValue(string html, string inputName)
    {
        var inputMatch = Regex.Match(
            html,
            $"<input[^>]*name=\"{Regex.Escape(inputName)}\"[^>]*value=\"([^\"]*)\"[^>]*>",
            RegexOptions.IgnoreCase);

        Assert.True(inputMatch.Success, $"Could not find input '{inputName}' in login form.");
        return WebUtility.HtmlDecode(inputMatch.Groups[1].Value);
    }
}
