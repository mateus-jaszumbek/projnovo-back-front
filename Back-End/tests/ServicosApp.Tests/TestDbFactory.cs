using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using ServicosApp.Infrastructure.Data;

namespace ServicosApp.Tests;

internal static class TestDbFactory
{
    public static (AppDbContext Context, SqliteConnection Connection) CreateContext()
    {
        var connection = new SqliteConnection("Data Source=:memory:");
        connection.Open();

        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlite(connection)
            .Options;

        var context = new AppDbContext(options);
        context.Database.EnsureCreated();

        return (context, connection);
    }
}
