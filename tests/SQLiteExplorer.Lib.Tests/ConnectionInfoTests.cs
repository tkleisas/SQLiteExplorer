using Microsoft.Data.SqlClient;
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
    public void SqlServer_Defaults_EncryptAndTrustServerCertificate()
    {
        var info = new SqlServerConnectionInfo { Server = "localhost", Database = "shop" };

        var cs = info.ConnectionString;

        Assert.Contains("Encrypt=True", cs);
        Assert.Contains("Trust Server Certificate=True", cs);
    }

    [Fact]
    public void SqlServer_EncryptModes_AreEmitted()
    {
        var plaintext = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            Encrypt = SqlConnectionEncryptOption.Optional,
            TrustServerCertificate = false
        };
        Assert.Contains("Encrypt=False", plaintext.ConnectionString);
        Assert.Contains("Trust Server Certificate=False", plaintext.ConnectionString);

        var strict = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            Encrypt = SqlConnectionEncryptOption.Strict
        };
        Assert.Contains("Encrypt=Strict", strict.ConnectionString);
        // Strict always validates the certificate and does not honour the keyword.
        Assert.DoesNotContain("Trust Server Certificate", strict.ConnectionString);
    }

    [Fact]
    public void SqlServer_TimeoutApplicationNameAndMars_AreEmitted()
    {
        var info = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            ConnectionTimeout = 5,
            ApplicationName = "NVS Database Explorer",
            MultipleActiveResultSets = true
        };

        var cs = info.ConnectionString;

        Assert.Contains("Connect Timeout=5", cs);
        // Values containing spaces are quoted by the builder.
        Assert.Contains("Application Name=\"NVS Database Explorer\"", cs);
        Assert.Contains("Multiple Active Result Sets=True", cs);
    }

    [Fact]
    public void SqlServer_AdditionalOptions_OverrideBuiltIns()
    {
        var info = new SqlServerConnectionInfo
        {
            Server = "localhost",
            Database = "shop",
            AdditionalOptions = "Packet Size=8192; Connect Timeout=3"
        };

        var cs = info.ConnectionString;

        Assert.Contains("Packet Size=8192", cs);
        Assert.Contains("Connect Timeout=3", cs);
        Assert.DoesNotContain("Connect Timeout=15", cs);
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
