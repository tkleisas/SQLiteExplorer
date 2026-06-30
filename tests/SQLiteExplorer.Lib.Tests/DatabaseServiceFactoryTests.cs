using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class DatabaseServiceFactoryTests
{
    [Theory]
    [InlineData(DatabaseType.SQLite, typeof(SqliteService))]
    [InlineData(DatabaseType.PostgreSQL, typeof(PostgresService))]
    [InlineData(DatabaseType.SqlServer, typeof(SqlServerService))]
    [InlineData(DatabaseType.Oracle, typeof(OracleService))]
    public void Create_ReturnsServiceMatchingDatabaseType(DatabaseType type, Type expected)
    {
        using var service = DatabaseServiceFactory.Create(type);

        Assert.IsType(expected, service);
        Assert.Equal(type, service.DatabaseType);
    }

    [Fact]
    public void Create_UnsupportedType_Throws()
    {
        Assert.Throws<ArgumentException>(() => DatabaseServiceFactory.Create((DatabaseType)999));
    }
}
