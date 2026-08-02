using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Tests;

public class ReportWizardViewModelTests : IDisposable
{
    private sealed class FakeLlmService : ILlmService
    {
        public bool IsConfigured { get; set; } = true;
        public string Reply { get; set; } = "```sql\nSELECT * FROM sales;\n```";

        public Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
            => Task.FromResult(Reply);
    }

    private sealed class FakeDatabaseService : IDatabaseService
    {
        public ConnectionInfo? ConnectionInfo => null;
        public bool IsConnected => true;
        public DatabaseType DatabaseType => DatabaseType.SQLite;
        public bool UsesSchemas => false;
        public string? LastSql;

        public Task<bool> ConnectAsync(ConnectionInfo connectionInfo) => Task.FromResult(true);
        public void Disconnect() { }
        public void Dispose() { }
        public Task<DatabaseInfo> GetDatabaseInfoAsync() => Task.FromResult(new DatabaseInfo());
        public string QuoteIdentifier(string? schema, string name) => $"\"{name}\"";
        public string GetDescribeSql(string? schema, string name) => $"PRAGMA table_info(\"{name}\")";

        public Task<MultiQueryResult> ExecuteMultipleAsync(string sql)
        {
            LastSql = sql;
            var result = new QueryResult
            {
                ColumnNames = new List<string> { "Id" },
                Rows = Enumerable.Range(1, 25)
                    .Select(i => new Dictionary<string, object?> { ["Id"] = i })
                    .ToList()
            };
            return Task.FromResult(new MultiQueryResult { Results = { result } });
        }
    }

    private readonly string _dir;
    private readonly FakeLlmService _llm = new();
    private readonly FakeDatabaseService _db = new();
    private readonly ReportStore _store;

    public ReportWizardViewModelTests()
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

    private ReportWizardViewModel CreateVm(ReportDefinition? existing = null) =>
        new(() => _llm, () => "schema", () => _db, _store, existing);

    [Fact]
    public async Task GenerateSqlWithAi_FillsSqlFromFencedResponse()
    {
        var vm = CreateVm();
        vm.AiPrompt = "all sales";

        await vm.GenerateSqlWithAiCommand.ExecuteAsync(null);

        Assert.Equal("SELECT * FROM sales;", vm.Sql);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task GenerateSqlWithAi_NotConfigured_SetsError()
    {
        _llm.IsConfigured = false;
        var vm = CreateVm();
        vm.AiPrompt = "anything";

        await vm.GenerateSqlWithAiCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.Equal(string.Empty, vm.Sql);
    }

    [Fact]
    public async Task Preview_RunsQueryAndCapsRows()
    {
        var vm = CreateVm();
        vm.Sql = "SELECT Id FROM t";

        await vm.PreviewCommand.ExecuteAsync(null);

        Assert.Equal("SELECT Id FROM t", _db.LastSql);
        var previewLines = vm.PreviewText.Split('\n', StringSplitOptions.RemoveEmptyEntries);
        // header + separator + capped rows
        Assert.Equal(2 + ReportWizardViewModel.MaxPreviewRows, previewLines.Length);
        Assert.Contains("25 row(s)", vm.StatusMessage);
        Assert.False(vm.HasError);
    }

    [Fact]
    public void Finish_ValidInput_SavesReport()
    {
        var vm = CreateVm();
        vm.Name = "My report";
        vm.Sql = "SELECT 1";

        vm.FinishCommand.Execute(null);

        Assert.NotNull(vm.SavedReport);
        var loaded = _store.Load();
        var saved = Assert.Single(loaded);
        Assert.Equal("My report", saved.Name);
        Assert.Equal("SELECT 1", saved.Sql);
    }

    [Fact]
    public void Finish_MissingName_SetsErrorAndDoesNotSave()
    {
        var vm = CreateVm();
        vm.Sql = "SELECT 1";

        vm.FinishCommand.Execute(null);

        Assert.True(vm.HasError);
        Assert.Null(vm.SavedReport);
        Assert.Empty(_store.Load());
    }

    [Fact]
    public void Finish_ExistingReport_UpdatesSameId()
    {
        var existing = new ReportDefinition { Name = "old", Sql = "SELECT 0" };
        _store.Save(existing);

        var vm = CreateVm(existing);
        Assert.Equal("old", vm.Name);

        vm.Name = "new";
        vm.FinishCommand.Execute(null);

        var loaded = _store.Load();
        var saved = Assert.Single(loaded);
        Assert.Equal(existing.Id, saved.Id);
        Assert.Equal("new", saved.Name);
    }
}
