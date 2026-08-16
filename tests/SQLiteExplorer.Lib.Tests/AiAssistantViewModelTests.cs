using SQLiteExplorer.Lib.Services;
using SQLiteExplorer.Lib.ViewModels;

namespace SQLiteExplorer.Lib.Tests;

public class AiAssistantViewModelTests
{
    private sealed class FakeLlmService : ILlmService
    {
        public bool IsConfigured { get; set; } = true;
        public string? LastSystemPrompt;
        public string? LastUserPrompt;
        public string Reply { get; set; } = "```sql\nSELECT 1;\n```";

        public Task<string> ChatAsync(string systemPrompt, string userPrompt, CancellationToken ct = default)
        {
            LastSystemPrompt = systemPrompt;
            LastUserPrompt = userPrompt;
            return Task.FromResult(Reply);
        }

        public bool StreamingCalled;
        public List<string>? StreamTokens;
        public bool HangUntilCancelled;

        public async Task<string> ChatStreamingAsync(
            string systemPrompt,
            string userPrompt,
            Action<string>? onToken,
            CancellationToken ct = default)
        {
            LastSystemPrompt = systemPrompt;
            LastUserPrompt = userPrompt;
            StreamingCalled = true;
            if (StreamTokens is not null)
            {
                foreach (var token in StreamTokens)
                {
                    onToken?.Invoke(token);
                }
            }
            if (HangUntilCancelled)
            {
                await Task.Delay(Timeout.Infinite, ct);
            }
            return Reply;
        }
    }

    private static (AiAssistantViewModel Vm, FakeLlmService Llm) Create(string schema = "schema text")
    {
        var llm = new FakeLlmService();
        var vm = new AiAssistantViewModel(() => llm, () => schema);
        return (vm, llm);
    }

    [Fact]
    public async Task Ask_SendsSchemaAndQuestion_FillsResponse()
    {
        var (vm, llm) = Create();
        vm.Question = "top 10 products";

        await vm.AskCommand.ExecuteAsync(null);

        Assert.Equal("```sql\nSELECT 1;\n```", vm.Response);
        Assert.Contains("top 10 products", llm.LastUserPrompt);
        Assert.Contains("schema text", llm.LastUserPrompt);
        Assert.False(vm.HasError);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Ask_EmptyQuestion_DoesNotCallLlm()
    {
        var (vm, llm) = Create();

        await vm.AskCommand.ExecuteAsync(null);

        Assert.Null(llm.LastUserPrompt);
    }

    [Fact]
    public async Task Explain_SendsCurrentSql()
    {
        var (vm, llm) = Create();

        await vm.ExplainAsync("SELECT * FROM Products");

        Assert.Contains("SELECT * FROM Products", llm.LastUserPrompt);
    }

    [Fact]
    public async Task Analyze_IncludesSampleRows()
    {
        var (vm, llm) = Create();
        var rows = new List<Dictionary<string, object?>>
        {
            new() { ["Name"] = "Widget" }
        };

        await vm.AnalyzeAsync("SELECT Name FROM T", new List<string> { "Name" }, rows);

        Assert.Contains("Widget", llm.LastUserPrompt);
    }

    [Fact]
    public async Task Ask_UsesStreaming_WhenServiceSupportsIt()
    {
        var (vm, llm) = Create();
        llm.Reply = "SELECT 1;";
        llm.StreamTokens = new List<string> { "SELECT ", "1;" };
        vm.Question = "top 10 products";

        await vm.AskCommand.ExecuteAsync(null);

        Assert.True(llm.StreamingCalled);
        Assert.Equal("SELECT 1;", vm.Response);
        Assert.False(vm.IsBusy);
    }

    [Fact]
    public async Task Stop_CancelsInFlightRequest_AndMarksResponse()
    {
        var (vm, llm) = Create();
        llm.HangUntilCancelled = true;
        llm.StreamTokens = new List<string> { "partial " };
        vm.Question = "q";

        var task = vm.AskCommand.ExecuteAsync(null);
        // Let the fake start delivering tokens and block on the cancellation token.
        await Task.Delay(50);
        vm.StopCommand.Execute(null);
        await task;

        Assert.Contains("partial ", vm.Response);
        Assert.Contains("(cancelled)", vm.Response);
        Assert.False(vm.IsBusy);
        Assert.False(vm.HasError);
    }

    [Fact]
    public async Task NotConfigured_SetsErrorAndSkipsCall()
    {
        var (vm, llm) = Create();
        llm.IsConfigured = false;
        vm.Question = "anything";

        await vm.AskCommand.ExecuteAsync(null);

        Assert.True(vm.HasError);
        Assert.NotEmpty(vm.ErrorMessage);
        Assert.Null(llm.LastUserPrompt);
    }

    [Fact]
    public async Task SqlFromResponse_ExtractsFencedSql()
    {
        var (vm, _) = Create();
        vm.Question = "q";

        await vm.AskCommand.ExecuteAsync(null);

        Assert.Equal("SELECT 1;", vm.SqlFromResponse);
        Assert.True(vm.HasSqlInResponse);
    }

    [Fact]
    public async Task Clear_ResetsState()
    {
        var (vm, _) = Create();
        vm.Question = "q";
        await vm.AskCommand.ExecuteAsync(null);

        vm.ClearCommand.Execute(null);

        Assert.Equal(string.Empty, vm.Question);
        Assert.Equal(string.Empty, vm.Response);
        Assert.False(vm.HasSqlInResponse);
    }
}
