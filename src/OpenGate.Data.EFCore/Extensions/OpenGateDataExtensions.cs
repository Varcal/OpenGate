using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using OpenGate.Data.EFCore.Entities;

namespace OpenGate.Data.EFCore.Extensions;

/// <summary>
/// Extension methods for registering OpenGate EF Core data services.
/// </summary>
public static class OpenGateDataExtensions
{
    /// <summary>
    /// Registers <see cref="OpenGateDbContext"/> and the ASP.NET Core Identity
    /// stores backed by EF Core.
    /// </summary>
    /// <param name="services">The service collection.</param>
    /// <param name="optionsAction">
    /// Action to configure the <see cref="DbContextOptions"/>.
    /// Use <c>options.UseSqlServer(...)</c>, <c>options.UseNpgsql(...)</c>, etc.
    /// </param>
    /// <returns>
    /// An <see cref="IdentityBuilder"/> that can be used to chain additional
    /// Identity configuration (e.g. <c>.AddDefaultTokenProviders()</c>).
    /// </returns>
    /// <example>
    /// <code>
    /// services.AddOpenGateData(options =>
    ///     options.UseSqlServer(connectionString));
    /// </code>
    /// </example>
    public static IdentityBuilder AddOpenGateData(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction)
        => services.AddOpenGateData<OpenGateDbContext, OpenGateUser, IdentityRole>(optionsAction, configureIdentity: null);

    /// <summary>
    /// Registers a custom DbContext and fully custom ASP.NET Core Identity
    /// user/role types backed by EF Core.
    /// </summary>
    public static IdentityBuilder AddOpenGateData<TContext, TUser, TRole>(
        this IServiceCollection services,
        Action<DbContextOptionsBuilder> optionsAction,
        Action<IdentityOptions>? configureIdentity = null)
        where TContext : DbContext
        where TUser : class
        where TRole : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(optionsAction);

        services.AddDbContext<TContext>(optionsAction);

        return services
            .AddIdentityCore<TUser>(BuildIdentityOptionsAction(configureIdentity))
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
    }

    /// <summary>
    /// Registers <see cref="OpenGateDbContext"/> using an already-configured
    /// <see cref="DbContextOptionsBuilder{TContext}"/> (useful for testing).
    /// </summary>
    public static IdentityBuilder AddOpenGateData<TContext>(
        this IServiceCollection services)
        where TContext : DbContext
        => services.AddOpenGateData<TContext, OpenGateUser, IdentityRole>(configureIdentity: null);

    /// <summary>
    /// Registers a pre-configured custom DbContext and fully custom ASP.NET Core
    /// Identity user/role types.
    /// </summary>
    public static IdentityBuilder AddOpenGateData<TContext, TUser, TRole>(
        this IServiceCollection services,
        Action<IdentityOptions>? configureIdentity = null)
        where TContext : DbContext
        where TUser : class
        where TRole : class
    {
        return services
            .AddIdentityCore<TUser>(BuildIdentityOptionsAction(configureIdentity))
            .AddRoles<TRole>()
            .AddEntityFrameworkStores<TContext>();
    }

    private static Action<IdentityOptions> BuildIdentityOptionsAction(
        Action<IdentityOptions>? configureIdentity)
    {
        return options =>
        {
            ConfigureIdentityDefaults(options);
            configureIdentity?.Invoke(options);
        };
    }

    private static void ConfigureIdentityDefaults(IdentityOptions options)
    {
        // Password policy
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.Password.RequiredLength = 12;

        // Lockout: lock after 5 failed attempts for 15 minutes
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
        options.Lockout.AllowedForNewUsers = true;

        // Require confirmed email before sign-in
        options.SignIn.RequireConfirmedEmail = false; // relaxed for MVP; set true in Production preset

        // Unique email
        options.User.RequireUniqueEmail = true;
    }
}

