using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.Tests;

public class ReportStoreTests : IDisposable
{
    private readonly string _dir;
    private readonly ReportStore _store;

    public ReportStoreTests()
    {
        _dir = Path.Combine(Path.GetTempPath(), "SQLiteExplorerTests", Guid.NewGuid().ToString("N"));
        _store = new ReportStore(_dir);
    }

    public void Dispose()
    {
        if (Directory.Exists(_dir))
        {
            Directory.Delete(_dir, recursive: true);
        }
    }

    [Fact]
    public void Load_NoFile_ReturnsEmpty()
    {
        Assert.Empty(_store.Load());
    }

    [Fact]
    public void Save_ThenLoad_RoundTrips()
    {
        var report = new ReportDefinition
        {
            Name = "Sales by month",
            Description = "Monthly totals",
            Sql = "SELECT * FROM sales"
        };

        _store.Save(report);
        var loaded = _store.Load();

        var found = Assert.Single(loaded);
        Assert.Equal(report.Id, found.Id);
        Assert.Equal("Sales by month", found.Name);
        Assert.Equal("SELECT * FROM sales", found.Sql);
    }

    [Fact]
    public void Save_ExistingId_UpdatesInsteadOfDuplicating()
    {
        var report = new ReportDefinition { Name = "v1", Sql = "SELECT 1" };
        _store.Save(report);

        report.Name = "v2";
        _store.Save(report);

        var loaded = _store.Load();
        var found = Assert.Single(loaded);
        Assert.Equal("v2", found.Name);
    }

    [Fact]
    public void Delete_RemovesReport()
    {
        var report = new ReportDefinition { Name = "x", Sql = "SELECT 1" };
        _store.Save(report);

        _store.Delete(report.Id);

        Assert.Empty(_store.Load());
    }

    [Fact]
    public void Load_CorruptFile_ReturnsEmpty()
    {
        Directory.CreateDirectory(_dir);
        File.WriteAllText(Path.Combine(_dir, "reports.json"), "{ not json !!");

        Assert.Empty(_store.Load());
    }
}
