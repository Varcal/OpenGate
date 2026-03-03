using Microsoft.EntityFrameworkCore;
using OpenGate.Data.EFCore;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.Data.EFCore.Tests;

public sealed class OpenGateDbContextTests : IDisposable
{
    private readonly OpenGateDbContext _db;

    public OpenGateDbContextTests()
    {
        var options = new DbContextOptionsBuilder<OpenGateDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _db = new OpenGateDbContext(options);
        _db.Database.EnsureCreated();
    }

    public void Dispose() => _db.Dispose();

    // ── OpenGateUser ──────────────────────────────────────────────────────────

    [Fact]
    public async Task User_CanBeInsertedAndRetrieved()
    {
        var user = new OpenGateUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "alice",
            Email = "alice@example.com",
            IsActive = true
        };

        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var found = await _db.Users.SingleOrDefaultAsync(u => u.UserName == "alice");
        Assert.NotNull(found);
        Assert.True(found.IsActive);
    }

    [Fact]
    public async Task User_DefaultValues_AreSet()
    {
        var before = DateTimeOffset.UtcNow;

        var user = new OpenGateUser
        {
            Id = Guid.NewGuid().ToString(),
            UserName = "bob"
        };

        Assert.True(user.IsActive);
        Assert.True(user.CreatedAt >= before);
        Assert.Null(user.LastLoginAt);
    }

    // ── UserProfile ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UserProfile_CanBeCreatedForUser()
    {
        var user = new OpenGateUser { Id = Guid.NewGuid().ToString(), UserName = "carol" };
        var profile = new UserProfile
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            FirstName = "Carol",
            LastName = "Smith",
            Locale = "en-US",
            TimeZone = "America/New_York"
        };

        _db.Users.Add(user);
        _db.UserProfiles.Add(profile);
        await _db.SaveChangesAsync();

        var found = await _db.UserProfiles
            .Include(p => p.User)
            .SingleOrDefaultAsync(p => p.UserId == user.Id);

        Assert.NotNull(found);
        Assert.Equal("Carol", found.FirstName);
        Assert.Equal("carol", found.User.UserName);
    }

    // ── AuditLog ──────────────────────────────────────────────────────────────

    [Fact]
    public async Task AuditLog_CanBeStoredWithoutUser()
    {
        var entry = new AuditLog
        {
            EventType = "Login.Failed",
            IpAddress = "192.168.1.1",
            Succeeded = false,
            Details = """{"reason":"invalid_credentials"}"""
        };

        _db.AuditLogs.Add(entry);
        await _db.SaveChangesAsync();

        var found = await _db.AuditLogs.FirstOrDefaultAsync(a => a.EventType == "Login.Failed");
        Assert.NotNull(found);
        Assert.Null(found.UserId);
        Assert.False(found.Succeeded);
    }

    [Fact]
    public async Task AuditLog_CanBeLinkedToUser()
    {
        var user = new OpenGateUser { Id = Guid.NewGuid().ToString(), UserName = "dave" };
        _db.Users.Add(user);
        await _db.SaveChangesAsync();

        var log = new AuditLog
        {
            UserId = user.Id,
            EventType = "Login.Success",
            ClientId = "spa-client",
            Succeeded = true
        };

        _db.AuditLogs.Add(log);
        await _db.SaveChangesAsync();

        var found = await _db.AuditLogs
            .Include(a => a.User)
            .FirstOrDefaultAsync(a => a.EventType == "Login.Success");

        Assert.NotNull(found);
        Assert.Equal("dave", found.User!.UserName);
    }

    // ── UserSession ───────────────────────────────────────────────────────────

    [Fact]
    public async Task UserSession_IsActive_WhenNotRevokedAndNotExpired()
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
        };

        Assert.True(session.IsActive);
    }

    [Fact]
    public async Task UserSession_IsNotActive_WhenRevoked()
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(1),
            RevokedAt = DateTimeOffset.UtcNow.AddMinutes(-5)
        };

        Assert.False(session.IsActive);
    }

    [Fact]
    public async Task UserSession_IsNotActive_WhenExpired()
    {
        var session = new UserSession
        {
            Id = Guid.NewGuid(),
            UserId = Guid.NewGuid().ToString(),
            ExpiresAt = DateTimeOffset.UtcNow.AddHours(-1)
        };

        Assert.False(session.IsActive);
    }
}

