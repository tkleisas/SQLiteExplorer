using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Tests;

public class ConnectionInfoTests
{
    [Fact]
    public void Postgres_ConnectionString_ContainsAllParts()
    {
        var info = new PostgresConnectionInfo
        {
            Host = "db.example.com",
            Port = 5544,
            Database = "shop",
            Username = "alice",
            Password = "secret"
        };

        var cs = info.ConnectionString;

        Assert.Contains("Host=db.example.com", cs);
        Assert.Contains("Port=5544", cs);
        Assert.Contains("Database=shop", cs);
        Assert.Contains("Username=alice", cs);
        Assert.Contains("Password=secret", cs);
        Assert.Equal("shop", info.DisplayName);
        Assert.Equal(DatabaseType.PostgreSQL, info.DatabaseType);
    }

    [Fact]
    public void SqlServer_SqlAuth_UsesUserIdAndPassword()
    {
        var info = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            UseWindowsAuth = false,
            Username = "sa",
            Password = "p@ss"
        };

        var cs = info.ConnectionString;

        Assert.Contains("Data Source=localhost", cs);
        Assert.Contains("Initial Catalog=shop", cs);
        Assert.Contains("User ID=sa", cs);
        Assert.Contains("Password=p@ss", cs);
        // Always trust the server cert so local/dev connections work.
        Assert.Contains("Trust Server Certificate=True", cs);
        Assert.DoesNotContain("Integrated Security", cs);
    }

    [Fact]
    public void SqlServer_WindowsAuth_UsesIntegratedSecurity_NotCredentials()
    {
        var info = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            UseWindowsAuth = true,
            Username = "ignored",
            Password = "ignored"
        };

        var cs = info.ConnectionString;

        Assert.Contains("Integrated Security=True", cs);
        Assert.DoesNotContain("User ID", cs);
    }

    [Fact]
    public void Oracle_EzConnect_BuildsHostPortServiceDescriptor()
    {
        var info = new OracleConnectionInfo
        {
            Host = "ora.example.com",
            Port = 1521,
            ServiceName = "FREEPDB1",
            Username = "scott",
            Password = "tiger"
        };

        var cs = info.ConnectionString;

        Assert.Contains("User Id=scott", cs);
        Assert.Contains("Password=tiger", cs);
        Assert.Contains("Data Source=ora.example.com:1521/FREEPDB1", cs);
        Assert.Equal("FREEPDB1", info.DisplayName);
    }

    [Fact]
    public void Oracle_RawConnectionString_IsUsedVerbatim()
    {
        var raw = "User Id=scott;Password=tiger;Data Source=MY_TNS_ALIAS";
        var info = new OracleConnectionInfo { RawConnectionString = raw };

        Assert.Equal(raw, info.ConnectionString);
    }

    [Fact]
    public void Sqlite_DisplayName_IsFileName()
    {
        var info = new SqliteConnectionInfo { FilePath = "/data/app/shop.db" };

        Assert.Equal("shop.db", info.DisplayName);
        Assert.Equal(DatabaseType.SQLite, info.DatabaseType);
    }
}
