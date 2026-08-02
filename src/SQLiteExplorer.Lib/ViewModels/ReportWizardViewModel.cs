using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.ViewModels;

/// <summary>
/// Backs the report wizard: details → SQL (optionally AI-generated) → preview → save.
/// </summary>
public partial class ReportWizardViewModel : ViewModelBase
{
    public const int MaxPreviewRows = 10;

    private readonly Func<ILlmService> _llmServiceFactory;
    private readonly Func<string> _schemaDescription;
    private readonly Func<IDatabaseService?> _databaseServiceFactory;
    private readonly ReportStore _store;
    private readonly ReportDefinition? _existing;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _sql = string.Empty;

    [ObservableProperty]
    private string _aiPrompt = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private string _previewText = string.Empty;

    public ReportWizardViewModel(
        Func<ILlmService> llmServiceFactory,
        Func<string> schemaDescription,
        Func<IDatabaseService?> databaseServiceFactory,
        ReportStore store,
        ReportDefinition? existing = null)
    {
        _llmServiceFactory = llmServiceFactory;
        _schemaDescription = schemaDescription;
        _databaseServiceFactory = databaseServiceFactory;
        _store = store;
        _existing = existing;

        if (existing != null)
        {
            Name = existing.Name;
            Description = existing.Description;
            Sql = existing.Sql;
        }
    }

    public bool IsAiAvailable => _llmServiceFactory().IsConfigured;

    /// <summary>Set when the wizard successfully saves; the dialog closes then.</summary>
    public ReportDefinition? SavedReport { get; private set; }

    [RelayCommand]
    private async Task GenerateSqlWithAi()
    {
        if (string.IsNullOrWhiteSpace(AiPrompt)) return;

        var llm = _llmServiceFactory();
        if (!llm.IsConfigured)
        {
            SetError("LLM is not configured. Open AI Settings (⚙) to set an endpoint and model.");
            return;
        }

        IsBusy = true;
        HasError = false;
        StatusMessage = string.Empty;

        try
        {
            var (system, user) = LlmPrompts.BuildGenerateSql(_schemaDescription(), AiPrompt.Trim());
            var response = await llm.ChatAsync(system, user);
            var sql = LlmPrompts.ExtractSql(response);
            if (string.IsNullOrWhiteSpace(sql))
            {
                SetError("The AI returned an empty query.");
            }
            else
            {
                Sql = sql;
            }
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task Preview()
    {
        if (string.IsNullOrWhiteSpace(Sql)) return;

        var db = _databaseServiceFactory();
        if (db == null || !db.IsConnected)
        {
            SetError("No database connected");
            return;
        }

        IsBusy = true;
        HasError = false;
        StatusMessage = string.Empty;

        try
        {
            var multiResult = await db.ExecuteMultipleAsync(Sql);
            var result = multiResult.Results.FirstOrDefault(r => r.IsSuccess);

            if (result == null)
            {
                SetError(multiResult.FirstError ?? "Query failed");
                PreviewText = string.Empty;
                return;
            }

            PreviewText = FormatPreview(result);
            StatusMessage = $"{result.RowCount} row(s) — preview shows first {Math.Min(MaxPreviewRows, result.RowCount)}";
        }
        catch (Exception ex)
        {
            SetError(ex.Message);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private void Finish()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            SetError("Give the report a name.");
            return;
        }

        if (string.IsNullOrWhiteSpace(Sql))
        {
            SetError("The report needs a SQL query.");
            return;
        }

        var report = _existing ?? new ReportDefinition();
        report.Name = Name.Trim();
        report.Description = Description.Trim();
        report.Sql = Sql.Trim();
        report.ModifiedUtc = DateTime.UtcNow;

        _store.Save(report);
        SavedReport = report;
    }

    internal static string FormatPreview(QueryResult result)
    {
        var sb = new StringBuilder();
        var columns = result.ColumnNames;
        sb.AppendLine(string.Join(" | ", columns));
        sb.AppendLine(new string('-', Math.Min(80, columns.Sum(c => c.Length + 3))));

        foreach (var row in result.Rows.Take(MaxPreviewRows))
        {
            var values = columns.Select(c =>
            {
                var s = row.TryGetValue(c, out var v) ? v?.ToString() ?? "NULL" : "";
                return s.Replace('\n', ' ').Replace('\r', ' ');
            });
            sb.AppendLine(string.Join(" | ", values));
        }

        return sb.ToString();
    }

    private void SetError(string message)
    {
        StatusMessage = message;
        HasError = true;
    }
}
