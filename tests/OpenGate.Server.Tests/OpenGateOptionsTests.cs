using OpenGate.Server.Options;

namespace OpenGate.Server.Tests;

/// <summary>
/// Unit tests for <see cref="OpenGateOptions"/> default values and
/// <see cref="OpenGateSecurityPreset"/> enum contract.
/// </summary>
public sealed class OpenGateOptionsTests
{
    // ── Default values ────────────────────────────────────────────────────────

    [Fact]
    public void Options_Default_Preset_Is_Production()
    {
        var options = new OpenGateOptions();
        Assert.Equal(OpenGateSecurityPreset.Production, options.SecurityPreset);
    }

    [Fact]
    public void Options_Default_AccessTokenLifetime_Is_OneHour()
    {
        var options = new OpenGateOptions();
        Assert.Equal(TimeSpan.FromHours(1), options.AccessTokenLifetime);
    }

    [Fact]
    public void Options_Default_RefreshTokenLifetime_Is_FourteenDays()
    {
        var options = new OpenGateOptions();
        Assert.Equal(TimeSpan.FromDays(14), options.RefreshTokenLifetime);
    }

    [Fact]
    public void Options_Default_AuthorizationCodeLifetime_Is_FiveMinutes()
    {
        var options = new OpenGateOptions();
        Assert.Equal(TimeSpan.FromMinutes(5), options.AuthorizationCodeLifetime);
    }

    [Fact]
    public void Options_Default_Endpoints_Match_Connect_Paths()
    {
        var options = new OpenGateOptions();

        Assert.Equal("/connect/authorize", options.AuthorizationEndpointPath);
        Assert.Equal("/connect/token", options.TokenEndpointPath);
        Assert.Equal("/connect/logout", options.LogoutEndpointPath);
        Assert.Equal("/connect/userinfo", options.UserinfoEndpointPath);
        Assert.Equal("/connect/introspect", options.IntrospectionEndpointPath);
        Assert.Equal("/connect/revoke", options.RevocationEndpointPath);
        Assert.Equal("/connect/device", options.DeviceEndpointPath);
    }

    [Fact]
    public void Options_Default_Features_Are_Enabled()
    {
        var options = new OpenGateOptions();

        Assert.True(options.EnableDeviceFlow);
        Assert.True(options.EnableIntrospection);
        Assert.True(options.EnableRevocation);
    }

    [Fact]
    public void Options_IssuerUri_Is_Null_By_Default()
    {
        var options = new OpenGateOptions();
        Assert.Null(options.IssuerUri);
    }

    // ── Mutation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Options_Can_Override_Preset()
    {
        var options = new OpenGateOptions { SecurityPreset = OpenGateSecurityPreset.Development };
        Assert.Equal(OpenGateSecurityPreset.Development, options.SecurityPreset);
    }

    [Fact]
    public void Options_Can_Override_Lifetimes()
    {
        var options = new OpenGateOptions
        {
            AccessTokenLifetime = TimeSpan.FromMinutes(30),
            RefreshTokenLifetime = TimeSpan.FromDays(7)
        };

        Assert.Equal(TimeSpan.FromMinutes(30), options.AccessTokenLifetime);
        Assert.Equal(TimeSpan.FromDays(7), options.RefreshTokenLifetime);
    }

    [Fact]
    public void Options_Can_Set_IssuerUri()
    {
        var uri = new Uri("https://identity.example.com");
        var options = new OpenGateOptions { IssuerUri = uri };

        Assert.Equal(uri, options.IssuerUri);
    }

    // ── SecurityPreset enum ───────────────────────────────────────────────────

    [Fact]
    public void SecurityPreset_Has_Three_Members()
    {
        var values = Enum.GetValues<OpenGateSecurityPreset>();
        Assert.Equal(3, values.Length);
    }

    [Theory]
    [InlineData(OpenGateSecurityPreset.Development)]
    [InlineData(OpenGateSecurityPreset.Production)]
    [InlineData(OpenGateSecurityPreset.HighSecurity)]
    public void SecurityPreset_AllValues_Are_Defined(OpenGateSecurityPreset preset)
    {
        Assert.True(Enum.IsDefined(preset));
    }
}

