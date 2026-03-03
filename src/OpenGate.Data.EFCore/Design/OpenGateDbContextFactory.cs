using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using OpenIddict.EntityFrameworkCore;

namespace OpenGate.Data.EFCore.Design;

/// <summary>
/// Used by <c>dotnet ef</c> CLI at design time to create
/// an <see cref="OpenGateDbContext"/> without a running host.
/// Not included in runtime or published packages.
/// </summary>
internal sealed class OpenGateDbContextFactory : IDesignTimeDbContextFactory<OpenGateDbContext>
{
    public OpenGateDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<OpenGateDbContext>();

        // Design-time only: use a placeholder connection string.
        // Actual connection strings are provided via appsettings / environment variables at runtime.
        optionsBuilder.UseSqlServer(
            "Server=(localdb)\\mssqllocaldb;Database=OpenGate_Design;Trusted_Connection=True;",
            sql => sql.MigrationsHistoryTable("__EFMigrationsHistory", "opengate"));

        // Ensure OpenIddict EF Core entities are included in the model when generating migrations.
        optionsBuilder.UseOpenIddict();

        return new OpenGateDbContext(optionsBuilder.Options);
    }
}

