using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ServicosApp.Infrastructure.Data;

public sealed class SqliteAppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var rawConnectionString =
            Environment.GetEnvironmentVariable("SQLITE_MIGRATIONS_CONNECTION")
            ?? "Data Source=servicosapp.dev.db;Default Timeout=60;";

        var connectionString = new SqliteConnectionStringBuilder(rawConnectionString)
        {
            DefaultTimeout = 60
        }.ToString();

        var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
        optionsBuilder.UseSqlite(connectionString);

        return new AppDbContext(optionsBuilder.Options);
    }
}
