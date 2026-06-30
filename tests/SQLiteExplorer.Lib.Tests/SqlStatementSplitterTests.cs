using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class SqlStatementSplitterTests
{
    [Fact]
    public void Split_SingleStatement_ReturnsOne()
    {
        var result = SqlStatementSplitter.Split("SELECT 1");

        Assert.Single(result);
        Assert.Equal("SELECT 1", result[0]);
    }

    [Fact]
    public void Split_MultipleStatements_SplitsOnSemicolons()
    {
        var result = SqlStatementSplitter.Split("SELECT 1; SELECT 2; SELECT 3");

        Assert.Equal(new[] { "SELECT 1", "SELECT 2", "SELECT 3" }, result);
    }

    [Fact]
    public void Split_TrailingSemicolon_DoesNotProduceEmptyStatement()
    {
        var result = SqlStatementSplitter.Split("SELECT 1;");

        Assert.Single(result);
        Assert.Equal("SELECT 1", result[0]);
    }

    [Fact]
    public void Split_EmptyOrWhitespaceBetweenSemicolons_IsIgnored()
    {
        var result = SqlStatementSplitter.Split("SELECT 1;;  ; SELECT 2");

        Assert.Equal(new[] { "SELECT 1", "SELECT 2" }, result);
    }

    [Fact]
    public void Split_SemicolonInsideStringLiteral_IsNotASeparator()
    {
        var result = SqlStatementSplitter.Split("SELECT 'a;b' AS x; SELECT 2");

        Assert.Equal(2, result.Count);
        Assert.Equal("SELECT 'a;b' AS x", result[0]);
    }

    [Fact]
    public void Split_EscapedSingleQuoteInsideLiteral_StaysInSameStatement()
    {
        var result = SqlStatementSplitter.Split("SELECT 'it''s; fine'; SELECT 2");

        Assert.Equal(2, result.Count);
        Assert.Equal("SELECT 'it''s; fine'", result[0]);
    }

    [Fact]
    public void Split_LineComment_IsStrippedAndSemicolonInItIgnored()
    {
        var result = SqlStatementSplitter.Split("SELECT 1 -- a comment ; not a split\n; SELECT 2");

        Assert.Equal(2, result.Count);
        Assert.Contains("SELECT 1", result[0]);
        Assert.DoesNotContain("comment", result[0]);
    }

    [Fact]
    public void Split_BlockComment_IsStrippedAndSemicolonInItIgnored()
    {
        var result = SqlStatementSplitter.Split("SELECT /* x; y */ 1; SELECT 2");

        Assert.Equal(2, result.Count);
        Assert.Contains("SELECT", result[0]);
        Assert.Contains("1", result[0]);
        Assert.DoesNotContain("x; y", result[0]);
    }

    [Fact]
    public void Split_DefaultMode_DoubleQuoteDoesNotProtectSemicolon()
    {
        // PostgreSQL/SQL Server/Oracle: double quotes delimit identifiers, not strings,
        // so they do not guard a contained semicolon.
        var result = SqlStatementSplitter.Split("SELECT \"a;b\"");

        Assert.Equal(2, result.Count);
    }

    [Fact]
    public void Split_SqliteMode_DoubleQuoteProtectsSemicolon()
    {
        // SQLite treats a double-quoted token as a possible string literal.
        var result = SqlStatementSplitter.Split("SELECT \"a;b\"", treatDoubleQuoteAsString: true);

        Assert.Single(result);
        Assert.Equal("SELECT \"a;b\"", result[0]);
    }

    [Fact]
    public void Split_EmptyInput_ReturnsEmpty()
    {
        Assert.Empty(SqlStatementSplitter.Split(""));
        Assert.Empty(SqlStatementSplitter.Split("   \n  "));
    }
}
