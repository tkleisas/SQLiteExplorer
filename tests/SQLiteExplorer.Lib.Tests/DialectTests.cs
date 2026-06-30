using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class DialectTests
{
    // --- UsesSchemas ---

    [Fact]
    public void Sqlite_DoesNotUseSchemas()
    {
        Assert.False(new SqliteService().UsesSchemas);
    }

    [Theory]
    [MemberData(nameof(SchemaServices))]
    public void ServerDatabases_UseSchemas(IDatabaseService service)
    {
        Assert.True(service.UsesSchemas);
    }

    public static IEnumerable<object[]> SchemaServices()
    {
        yield return new object[] { new PostgresService() };
        yield return new object[] { new SqlServerService() };
        yield return new object[] { new OracleService() };
    }

    // --- QuoteIdentifier ---

    [Fact]
    public void Sqlite_Quote_WrapsInDoubleQuotes_IgnoringSchema()
    {
        var svc = new SqliteService();
        Assert.Equal("\"users\"", svc.QuoteIdentifier(null, "users"));
        Assert.Equal("\"users\"", svc.QuoteIdentifier("ignored", "users"));
    }

    [Fact]
    public void Sqlite_Quote_EscapesEmbeddedDoubleQuotes()
    {
        Assert.Equal("\"a\"\"b\"", new SqliteService().QuoteIdentifier(null, "a\"b"));
    }

    [Fact]
    public void Postgres_Quote_SchemaQualifiedWithDoubleQuotes()
    {
        var svc = new PostgresService();
        Assert.Equal("\"users\"", svc.QuoteIdentifier(null, "users"));
        Assert.Equal("\"public\".\"users\"", svc.QuoteIdentifier("public", "users"));
    }

    [Fact]
    public void SqlServer_Quote_SchemaQualifiedWithBrackets()
    {
        var svc = new SqlServerService();
        Assert.Equal("[users]", svc.QuoteIdentifier(null, "users"));
        Assert.Equal("[dbo].[users]", svc.QuoteIdentifier("dbo", "users"));
    }

    [Fact]
    public void SqlServer_Quote_EscapesClosingBracket()
    {
        Assert.Equal("[a]]b]", new SqlServerService().QuoteIdentifier(null, "a]b"));
    }

    [Fact]
    public void Oracle_Quote_SchemaQualifiedWithDoubleQuotes()
    {
        Assert.Equal("\"HR\".\"EMPLOYEES\"", new OracleService().QuoteIdentifier("HR", "EMPLOYEES"));
    }

    // --- GetDescribeSql ---

    [Fact]
    public void Sqlite_Describe_UsesPragma()
    {
        Assert.Contains("PRAGMA table_info", new SqliteService().GetDescribeSql(null, "users"));
    }

    [Fact]
    public void Postgres_Describe_FiltersBySchemaAndTable()
    {
        var sql = new PostgresService().GetDescribeSql("public", "users");
        Assert.Contains("information_schema.columns", sql);
        Assert.Contains("'public'", sql);
        Assert.Contains("'users'", sql);
    }

    [Fact]
    public void SqlServer_Describe_UsesInformationSchema()
    {
        var sql = new SqlServerService().GetDescribeSql("dbo", "users");
        Assert.Contains("INFORMATION_SCHEMA.COLUMNS", sql);
        Assert.Contains("'dbo'", sql);
    }

    [Fact]
    public void Oracle_Describe_UsesAllTabColumns_AndHasNoTrailingSemicolon()
    {
        var sql = new OracleService().GetDescribeSql("HR", "EMPLOYEES");
        Assert.Contains("ALL_TAB_COLUMNS", sql);
        // A plain (non-PL/SQL) statement to OracleCommand must not end with ';'.
        Assert.False(sql.TrimEnd().EndsWith(";"));
    }
}
