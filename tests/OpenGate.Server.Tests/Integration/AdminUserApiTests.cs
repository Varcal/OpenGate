using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.WebUtilities;
using Microsoft.Extensions.DependencyInjection;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.Server.Tests.Integration;

public sealed class AdminUserApiTests(OpenGateWebFactory factory)
    : IClassFixture<OpenGateWebFactory>
{
    private const string CreatedUserEmail = "operator@opengate.test";
    private const string CreatedUserPassword = "Operator@1234!";
    private const string AuthorizationUrl = "/connect/authorize" +
        "?response_type=code" +
        "&client_id=interactive-demo" +
        "&redirect_uri=http%3A%2F%2Flocalhost%2Fcallback" +
        "&scope=openid%20email" +
        "&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" +
        "&code_challenge_method=S256";
    private const string AdminDashboardAuthorizationUrl = "/connect/authorize" +
        "?response_type=code" +
        "&client_id=admin-dashboard" +
        "&redirect_uri=http%3A%2F%2Flocalhost%2Fadmin%2Fcallback" +
        "&scope=openid%20email%20profile%20roles%20admin_api" +
        "&code_challenge=E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM" +
        "&code_challenge_method=S256";
    private const string PkceVerifier = "dBjftJeZ4CVP-mB92K27uhbUJU1p1r_wW1gFWFOEjXk";

    private static readonly string[] ViewerRole = ["Viewer"];
    private static readonly string[] AdminRole = ["Admin"];

    private readonly HttpClient _adminClient = factory.CreateClient(new() { AllowAutoRedirect = false });

    [Fact]
    public async Task AdminApi_Can_Create_Update_Assign_And_Delete_User()
    {
        await LoginAsync(_adminClient, IntegrationSeedService.DemoEmail, IntegrationSeedService.DemoPassword);

        var rolesResponse = await _adminClient.GetAsync("/admin/api/roles");
        var rolesJson = await rolesResponse.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(HttpStatusCode.OK, rolesResponse.StatusCode);
        Assert.NotNull(rolesJson);
        Assert.Contains(
            rolesJson.RootElement.GetProperty("items").EnumerateArray().Select(item => item.GetProperty("name").GetString()),
            role => role == "SuperAdmin");

        var createResponse = await _adminClient.PostAsJsonAsync("/admin/api/users", new
        {
            email = CreatedUserEmail,
            userName = "operator",
            password = CreatedUserPassword,
            emailConfirmed = true,
            firstName = "Open",
            lastName = "Gate",
            displayName = "Open Gate Operator",
            locale = "en-US",
            timeZone = "UTC",
            roles = ViewerRole
        });

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdJson);

        var userId = createdJson.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userId));
        Assert.Equal("operator", createdJson.RootElement.GetProperty("userName").GetString());
        Assert.Contains(
            createdJson.RootElement.GetProperty("roles").EnumerateArray().Select(item => item.GetString()),
            role => role == "Viewer");

        var updateResponse = await _adminClient.PutAsJsonAsync($"/admin/api/users/{userId}", new
        {
            displayName = "Open Gate Operator v2",
            locale = "pt-BR",
            timeZone = "America/Sao_Paulo",
            emailConfirmed = true,
            roles = AdminRole
        });

        var updatedJson = await updateResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updatedJson);
        Assert.Equal("Open Gate Operator v2", updatedJson.RootElement.GetProperty("profile").GetProperty("displayName").GetString());
        Assert.Contains(
            updatedJson.RootElement.GetProperty("roles").EnumerateArray().Select(item => item.GetString()),
            role => role == "Admin");

        var rolesUpdateResponse = await _adminClient.PutAsJsonAsync($"/admin/api/users/{userId}/roles", new
        {
            roles = ViewerRole
        });

        var rolesUpdatedJson = await rolesUpdateResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, rolesUpdateResponse.StatusCode);
        Assert.NotNull(rolesUpdatedJson);
        Assert.Contains(
            rolesUpdatedJson.RootElement.GetProperty("roles").EnumerateArray().Select(item => item.GetString()),
            role => role == "Viewer");

        var deleteResponse = await _adminClient.DeleteAsync($"/admin/api/users/{userId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var getDeletedResponse = await _adminClient.GetAsync($"/admin/api/users/{userId}");
        Assert.Equal(HttpStatusCode.NotFound, getDeletedResponse.StatusCode);
    }

    [Fact]
    public async Task Inactive_User_Cannot_Login_Or_Authorize()
    {
        await LoginAsync(_adminClient, IntegrationSeedService.DemoEmail, IntegrationSeedService.DemoPassword);

        var createResponse = await _adminClient.PostAsJsonAsync("/admin/api/users", new
        {
            email = "inactive@opengate.test",
            userName = "inactive-user",
            password = CreatedUserPassword,
            emailConfirmed = true,
            roles = ViewerRole
        });

        var createdJson = await createResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(createdJson);

        var userId = createdJson.RootElement.GetProperty("id").GetString();
        Assert.False(string.IsNullOrWhiteSpace(userId));

        var userClient = factory.CreateClient(new() { AllowAutoRedirect = false });
        var successfulLoginResponse = await LoginAsync(userClient, "inactive@opengate.test", CreatedUserPassword);
        Assert.Equal(HttpStatusCode.Redirect, successfulLoginResponse.StatusCode);

        var deactivateResponse = await _adminClient.PutAsJsonAsync($"/admin/api/users/{userId}", new
        {
            isActive = false
        });

        Assert.Equal(HttpStatusCode.OK, deactivateResponse.StatusCode);

        var blockedLoginClient = factory.CreateClient(new() { AllowAutoRedirect = false });
        var blockedLoginResponse = await LoginAsync(blockedLoginClient, "inactive@opengate.test", CreatedUserPassword);
        var blockedLoginHtml = await blockedLoginResponse.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, blockedLoginResponse.StatusCode);
        Assert.Contains("E-mail ou senha incorretos", blockedLoginHtml, StringComparison.OrdinalIgnoreCase);

        var authorizeResponse = await userClient.GetAsync(AuthorizationUrl);
        Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
        Assert.Contains("Login", authorizeResponse.Headers.Location?.ToString() ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Admin_User_Can_Call_AdminApi_With_AuthorizationCode_Bearer_Token()
    {
        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var accessToken = await RequestUserAccessTokenAsync(client, IntegrationSeedService.DemoEmail, IntegrationSeedService.DemoPassword);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/admin/api/me");
        var json = await response.Content.ReadFromJsonAsync<JsonDocument>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(json);
        Assert.Equal("user", json.RootElement.GetProperty("kind").GetString());
        Assert.Equal(IntegrationSeedService.DemoEmail, json.RootElement.GetProperty("email").GetString());
        Assert.Contains(
            json.RootElement.GetProperty("roles").EnumerateArray().Select(item => item.GetString()),
            role => role == "SuperAdmin");
    }

    [Fact]
    public async Task NonAdmin_User_Bearer_Token_Cannot_Call_AdminApi()
    {
        const string email = "frontend-user@opengate.test";
        const string password = "Frontend@1234!";

        await using (var scope = factory.Services.CreateAsyncScope())
        {
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<OpenGateUser>>();

            if (await userManager.FindByEmailAsync(email) is null)
            {
                var result = await userManager.CreateAsync(new OpenGateUser
                {
                    Email = email,
                    UserName = email,
                    EmailConfirmed = true,
                    IsActive = true
                }, password);

                Assert.True(result.Succeeded, string.Join("; ", result.Errors.Select(error => error.Description)));
            }
        }

        using var client = factory.CreateClient(new() { AllowAutoRedirect = false });
        var accessToken = await RequestUserAccessTokenAsync(client, email, password);

        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);

        var response = await client.GetAsync("/admin/api/me");
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    private static async Task<string> RequestUserAccessTokenAsync(HttpClient client, string email, string password)
    {
        var loginResponse = await LoginAsync(client, email, password);
        Assert.Equal(HttpStatusCode.Redirect, loginResponse.StatusCode);

        var authorizeResponse = await client.GetAsync(AdminDashboardAuthorizationUrl);
        string? location;

        if (authorizeResponse.StatusCode == HttpStatusCode.OK)
        {
            var consentHtml = await authorizeResponse.Content.ReadAsStringAsync();
            var antiforgeryToken = ExtractInputValue(consentHtml, "__RequestVerificationToken");

            var allowResponse = await client.PostAsync(AdminDashboardAuthorizationUrl, new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["response_type"] = "code",
                ["client_id"] = IntegrationSeedService.AdminDashboardClientId,
                ["redirect_uri"] = "http://localhost/admin/callback",
                ["scope"] = "openid email profile roles admin_api",
                ["code_challenge"] = "E9Melhoa2OwvFrEMTJguCHaoeK1t8URWbuGJSstw-cM",
                ["code_challenge_method"] = "S256",
                ["decision"] = "allow",
                ["__RequestVerificationToken"] = antiforgeryToken
            }));

            Assert.Equal(HttpStatusCode.Redirect, allowResponse.StatusCode);
            location = allowResponse.Headers.Location?.ToString();
        }
        else
        {
            Assert.Equal(HttpStatusCode.Redirect, authorizeResponse.StatusCode);
            location = authorizeResponse.Headers.Location?.ToString();
        }

        Assert.False(string.IsNullOrWhiteSpace(location));

        var callbackUri = new Uri(location!, UriKind.RelativeOrAbsolute);
        if (!callbackUri.IsAbsoluteUri)
        {
            callbackUri = new Uri(new Uri("http://localhost"), callbackUri);
        }

        var query = QueryHelpers.ParseQuery(callbackUri.Query);
        Assert.True(query.TryGetValue("code", out var codeValues));
        var code = codeValues.ToString();
        Assert.False(string.IsNullOrWhiteSpace(code));

        var tokenResponse = await client.PostAsync("/connect/token", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["grant_type"] = "authorization_code",
            ["client_id"] = IntegrationSeedService.AdminDashboardClientId,
            ["redirect_uri"] = "http://localhost/admin/callback",
            ["code"] = code,
            ["code_verifier"] = PkceVerifier
        }));

        var tokenJson = await tokenResponse.Content.ReadFromJsonAsync<JsonDocument>();
        Assert.Equal(HttpStatusCode.OK, tokenResponse.StatusCode);
        Assert.NotNull(tokenJson);

        return tokenJson.RootElement.GetProperty("access_token").GetString()
            ?? throw new InvalidOperationException("The token response did not contain an access token.");
    }

    private static async Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password)
    {
        var loginPage = await client.GetAsync("/Account/Login");
        var html = await loginPage.Content.ReadAsStringAsync();

        Assert.Equal(HttpStatusCode.OK, loginPage.StatusCode);

        var antiforgeryToken = ExtractInputValue(html, "__RequestVerificationToken");
        var returnUrl = ExtractInputValue(html, "ReturnUrl");

        return await client.PostAsync("/Account/Login", new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["Input.Email"] = email,
            ["Input.Password"] = password,
            ["Input.RememberMe"] = "false",
            ["ReturnUrl"] = string.IsNullOrWhiteSpace(returnUrl) ? "/" : returnUrl,
            ["__RequestVerificationToken"] = antiforgeryToken
        }));
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
