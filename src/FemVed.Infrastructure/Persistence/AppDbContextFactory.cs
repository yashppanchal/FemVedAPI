using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace FemVed.Infrastructure.Persistence;

/// <summary>
/// Design-time factory used by EF Core tools (migrations, scaffolding) so they
/// can create an <see cref="AppDbContext"/> without booting the full ASP.NET Core host.
/// The connection string is read from the <c>DB_CONNECTION_STRING</c> environment
/// variable, falling back to a local placeholder that allows the migration to be
/// generated even without a live database.
/// </summary>
public sealed class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    /// <inheritdoc />
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=localhost;Database=femved_db;Username=postgres;Password=postgres";

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseNpgsql(
                connectionString,
                npgsql => npgsql.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name))
            .Options;

        return new AppDbContext(options);
    }
}
