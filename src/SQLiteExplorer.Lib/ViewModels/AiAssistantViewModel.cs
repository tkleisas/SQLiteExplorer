using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.ViewModels;

/// <summary>
/// Backs the AI Assistant side panel: natural-language → SQL, query explanation,
/// optimization advice and result-set analysis. Uses the <see cref="ILlmService"/>
/// supplied by the host (or the built-in OpenAI-compatible one in the standalone app).
/// </summary>
public partial class AiAssistantViewModel : ViewModelBase
{
    private readonly Func<ILlmService> _llmServiceFactory;
    private readonly Func<string> _schemaDescription;
    private CancellationTokenSource? _cts;

    [ObservableProperty]
    private string _question = string.Empty;

    [ObservableProperty]
    private string _response = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _errorMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isVisible;

    public AiAssistantViewModel(Func<ILlmService> llmServiceFactory, Func<string> schemaDescription)
    {
        _llmServiceFactory = llmServiceFactory;
        _schemaDescription = schemaDescription;
    }

    /// <summary>SQL extracted from the current response, for "Insert into editor".</summary>
    public string SqlFromResponse => LlmPrompts.ExtractSql(Response);

    public bool HasSqlInResponse => !string.IsNullOrWhiteSpace(SqlFromResponse);

    [RelayCommand]
    private async Task Ask()
    {
        if (string.IsNullOrWhiteSpace(Question)) return;
        var (system, user) = LlmPrompts.BuildGenerateSql(_schemaDescription(), Question.Trim());
        await RunAsync(system, user);
    }

    [RelayCommand]
    private void Clear()
    {
        _cts?.Cancel();
        Question = string.Empty;
        Response = string.Empty;
        ErrorMessage = string.Empty;
        HasError = false;
        OnPropertyChanged(nameof(SqlFromResponse));
        OnPropertyChanged(nameof(HasSqlInResponse));
    }

    /// <summary>Cancels an in-flight request, keeping the partial response visible.</summary>
    [RelayCommand]
    private void Stop()
    {
        _cts?.Cancel();
    }

    public Task ExplainAsync(string sql)
    {
        var (system, user) = LlmPrompts.BuildExplain(_schemaDescription(), sql);
        return RunAsync(system, user);
    }

    public Task OptimizeAsync(string sql)
    {
        var (system, user) = LlmPrompts.BuildOptimize(_schemaDescription(), sql);
        return RunAsync(system, user);
    }

    public Task AnalyzeAsync(string sql, IReadOnlyList<string> columns, IReadOnlyList<Dictionary<string, object?>> rows)
    {
        var (system, user) = LlmPrompts.BuildAnalyzeResults(_schemaDescription(), sql, columns, rows);
        return RunAsync(system, user);
    }

    private async Task RunAsync(string systemPrompt, string userPrompt)
    {
        var llm = _llmServiceFactory();
        if (!llm.IsConfigured)
        {
            ErrorMessage = "LLM is not configured. Open AI Settings (⚙) to set an endpoint and model.";
            HasError = true;
            return;
        }

        _cts?.Cancel();
        _cts?.Dispose();
        _cts = new CancellationTokenSource();

        IsBusy = true;
        HasError = false;
        ErrorMessage = string.Empty;
        Response = string.Empty;

        try
        {
            // Stream tokens into the response as they arrive (services that don't
            // support streaming deliver the whole reply in one callback).
            var sb = new System.Text.StringBuilder();
            var reply = await llm.ChatStreamingAsync(
                systemPrompt,
                userPrompt,
                onToken: token =>
                {
                    sb.Append(token);
                    Response = sb.ToString();
                },
                _cts.Token);

            Response = reply;
            OnPropertyChanged(nameof(SqlFromResponse));
            OnPropertyChanged(nameof(HasSqlInResponse));
        }
        catch (OperationCanceledException)
        {
            Response = string.IsNullOrWhiteSpace(Response)
                ? "(cancelled)"
                : Response + System.Environment.NewLine + System.Environment.NewLine + "(cancelled)";
        }
        catch (Exception ex)
        {
            ErrorMessage = ex.Message;
            HasError = true;
        }
        finally
        {
            IsBusy = false;
            _cts?.Dispose();
            _cts = null;
        }
    }
}
