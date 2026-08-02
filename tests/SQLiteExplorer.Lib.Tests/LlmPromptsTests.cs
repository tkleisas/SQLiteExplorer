using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class LlmPromptsTests
{
    private static List<TableInfo> SampleTables() => new()
    {
        new TableInfo
        {
            Name = "Products",
            Schema = "",
            Columns = new List<ColumnInfo>
            {
                new() { Name = "Id", Type = "INTEGER" },
                new() { Name = "Name", Type = "TEXT" }
            }
        },
        new TableInfo
        {
            Name = "Orders",
            Schema = "sales",
            Columns = new List<ColumnInfo>
            {
                new() { Name = "Id", Type = "INTEGER" }
            }
        }
    };

    [Fact]
    public void BuildSchemaDescription_IncludesDialectTablesAndColumns()
    {
        var schema = LlmPrompts.BuildSchemaDescription("SQLite", SampleTables());

        Assert.Contains("SQLite", schema);
        Assert.Contains("Products", schema);
        Assert.Contains("Name TEXT", schema);
        Assert.Contains("sales.Orders", schema);
    }

    [Fact]
    public void BuildGenerateSql_IncludesSchemaAndQuestion()
    {
        var schema = LlmPrompts.BuildSchemaDescription("PostgreSQL", SampleTables());
        var (system, user) = LlmPrompts.BuildGenerateSql(schema, "top 10 products");

        Assert.Contains("PostgreSQL", user);
        Assert.Contains("top 10 products", user);
        Assert.Contains("SQL", system);
    }

    [Fact]
    public void BuildCompletion_IncludesSqlBeforeCaret()
    {
        var (_, user) = LlmPrompts.BuildCompletion("schema text", "SELECT * FROM Pr");

        Assert.Contains("schema text", user);
        Assert.Contains("SELECT * FROM Pr", user);
    }

    [Fact]
    public void BuildAnalyzeResults_CapsRowsAtMaximum()
    {
        var columns = new List<string> { "Id" };
        var rows = Enumerable.Range(1, LlmPrompts.MaxAnalysisRows + 50)
            .Select(i => new Dictionary<string, object?> { ["Id"] = i })
            .ToList();

        var (_, user) = LlmPrompts.BuildAnalyzeResults("schema", "SELECT Id FROM T", columns, rows);

        var lines = user.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        Assert.Contains(LlmPrompts.MaxAnalysisRows.ToString(), lines);
        Assert.DoesNotContain((LlmPrompts.MaxAnalysisRows + 1).ToString(), lines);
        Assert.Contains($"Total rows returned: {LlmPrompts.MaxAnalysisRows + 50}. Sample:", lines);
    }

    [Fact]
    public void ExtractSql_FencedBlock_ReturnsContents()
    {
        var text = "Here you go:\n```sql\nSELECT * FROM Products;\n```\nDone.";

        Assert.Equal("SELECT * FROM Products;", LlmPrompts.ExtractSql(text));
    }

    [Fact]
    public void ExtractSql_NoFence_ReturnsTrimmedText()
    {
        Assert.Equal("SELECT 1;", LlmPrompts.ExtractSql("  SELECT 1;  \n"));
    }

    [Fact]
    public void ExtractSql_Empty_ReturnsEmpty()
    {
        Assert.Equal(string.Empty, LlmPrompts.ExtractSql("   "));
    }
}
