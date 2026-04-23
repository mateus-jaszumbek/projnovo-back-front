using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Infrastructure.PostgresMigrations;

public sealed class PostgresAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connectionString =
            Environment.GetEnvironmentVariable("POSTGRES_MIGRATIONS_CONNECTION")
            ?? "Host=localhost;Port=5432;Database=servicosapp;Username=postgres;Password=postgres;SSL Mode=Disable";

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseNpgsql(
            connectionString,
            npgsql => npgsql.MigrationsAssembly(typeof(PostgresMigrationsMarker).Assembly.FullName));

        return new AppDbContext(optionsBuilder.Options);
    }
}
