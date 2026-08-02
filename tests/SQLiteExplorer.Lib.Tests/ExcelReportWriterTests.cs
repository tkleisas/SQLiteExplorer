using ClosedXML.Excel;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class ExcelReportWriterTests : IDisposable
{
    private readonly string _dir;

    public ExcelReportWriterTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SQLiteExplorerTests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Write_ProducesReadableWorkbookWithTitleHeadersAndTypedCells()
    {
        var result = new QueryResult
        {
            ColumnNames = new List<string> { "Id", "Name", "Price", "InStock" },
            Rows = new List<Dictionary<string, object?>>
            {
                new() { ["Id"] = 1L, ["Name"] = "Widget", ["Price"] = 9.99, ["InStock"] = true },
                new() { ["Id"] = 2L, ["Name"] = "Gadget", ["Price"] = 19.5, ["InStock"] = false }
            }
        };
        var report = new ReportDefinition { Name = "Products report", Sql = "SELECT ..." };
        var path = Path.Combine(_dir, "report.xlsx");

        ExcelReportWriter.Write(result, report, path);

        Assert.True(File.Exists(path));
        using var workbook = new XLWorkbook(path);
        var ws = workbook.Worksheet("Report");

        Assert.Equal("Products report", ws.Cell(1, 1).GetString());
        Assert.Contains("2 row(s)", ws.Cell(2, 1).GetString());

        Assert.Equal("Id", ws.Cell(5, 1).GetString());
        Assert.Equal("Name", ws.Cell(5, 2).GetString());
        Assert.True(ws.Cell(5, 1).Style.Font.Bold);

        Assert.Equal(1, ws.Cell(6, 1).GetValue<long>());
        Assert.Equal("Widget", ws.Cell(6, 2).GetString());
        Assert.Equal(9.99, ws.Cell(6, 3).GetValue<double>(), precision: 2);
        Assert.True(ws.Cell(6, 4).GetValue<bool>());
        Assert.Equal(2, ws.Cell(7, 1).GetValue<long>());
    }

    [Fact]
    public void Write_NullValues_ProduceEmptyCells()
    {
        var result = new QueryResult
        {
            ColumnNames = new List<string> { "A" },
            Rows = new List<Dictionary<string, object?>> { new() { ["A"] = null } }
        };
        var path = Path.Combine(_dir, "nulls.xlsx");

        ExcelReportWriter.Write(result, new ReportDefinition { Name = "n" }, path);

        using var workbook = new XLWorkbook(path);
        Assert.True(workbook.Worksheet("Report").Cell(6, 1).IsEmpty());
    }
}
