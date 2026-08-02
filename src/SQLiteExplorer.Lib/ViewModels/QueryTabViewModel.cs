using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using AvaloniaVirtualDataGrid.Columns;
using AvaloniaVirtualDataGrid.Controls;
using AvaloniaVirtualDataGrid.Core;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Lib.Completion;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.ViewModels;

public partial class QueryTabViewModel : ViewModelBase
{
    private readonly Func<IDatabaseService?> _databaseServiceFactory;

    [ObservableProperty]
    private string _title = "Query";

    [ObservableProperty]
    private string _sqlText = string.Empty;

    [ObservableProperty]
    private string _resultStatus = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isLoading;

    [ObservableProperty]
    private ObservableCollection<ResultSetViewModel> _resultSets = new();

    [ObservableProperty]
    private ResultSetViewModel? _selectedResultSet;

    public event EventHandler<string>? QueryExecuted;

    /// <summary>
    /// The editor control attached to this tab (set via EditorAdapterBehavior).
    /// Null when the tab's view has not been created yet.
    /// </summary>
    public SqlEditorAdapter? Editor { get; set; }

    public QueryTabViewModel(Func<IDatabaseService?> databaseServiceFactory)
    {
        _databaseServiceFactory = databaseServiceFactory;
    }

    /// <summary>
    /// Inserts SQL at the editor caret when the editor is attached;
    /// otherwise appends it to the SQL text.
    /// </summary>
    public void InsertSqlAtCaret(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return;

        if (Editor != null)
        {
            Editor.InsertAtCaret(sql);
        }
        else
        {
            SqlText = string.IsNullOrWhiteSpace(SqlText)
                ? sql
                : SqlText.TrimEnd() + Environment.NewLine + sql;
        }
    }

    [RelayCommand]
    private async Task ExecuteQuery()
    {
        if (string.IsNullOrWhiteSpace(SqlText)) return;
        
        var databaseService = _databaseServiceFactory();
        if (databaseService == null || !databaseService.IsConnected)
        {
            ResultStatus = "No database connected";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;
        ResultSets.Clear();
        SelectedResultSet = null;

        try
        {
            var multiResult = await databaseService.ExecuteMultipleAsync(SqlText);

            HasError = multiResult.HasErrors;

            if (multiResult.Results.Count == 0)
            {
                ResultStatus = "No statements executed";
            }
            else if (multiResult.Results.Count == 1)
            {
                var r = multiResult.Results[0];
                if (r.IsSuccess)
                    ResultStatus = $"{r.RowCount} row(s) in {r.ExecutionTimeMs}ms";
                else
                    ResultStatus = $"Error: {r.ErrorMessage}";
            }
            else
            {
                if (multiResult.HasErrors)
                {
                    var errorCount = multiResult.ErrorCount;
                    var successCount = multiResult.SuccessCount;
                    ResultStatus = $"{successCount} succeeded, {errorCount} failed - Total: {multiResult.TotalRows} row(s) in {multiResult.TotalExecutionTimeMs}ms";
                }
                else
                {
                    ResultStatus = $"{multiResult.Results.Count} statements - {multiResult.TotalRows} row(s) in {multiResult.TotalExecutionTimeMs}ms";
                }
            }

            for (var i = 0; i < multiResult.Results.Count; i++)
            {
                var resultSet = new ResultSetViewModel(multiResult.Results[i], i + 1);
                ResultSets.Add(resultSet);
            }

            if (ResultSets.Count > 0)
            {
                SelectedResultSet = ResultSets[0];
            }

            QueryExecuted?.Invoke(this, SqlText);
        }
        catch (Exception ex)
        {
            ResultStatus = $"Error: {ex.Message}";
            HasError = true;
        }
        finally
        {
            IsLoading = false;
        }
    }
}

public partial class ResultSetViewModel : ObservableObject
{
    private List<string> _columnNames = new();
    private List<Dictionary<string, object?>> _rows = new();

    /// <summary>Column names of the result set (empty for failed statements).</summary>
    public IReadOnlyList<string> ColumnNames => _columnNames;

    /// <summary>Rows of the result set (empty for failed statements).</summary>
    public IReadOnlyList<Dictionary<string, object?>> Rows => _rows;

    [ObservableProperty]
    private string _title;

    [ObservableProperty]
    private string _status;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private VirtualDataGridColumnCollection _columns = new();

    [ObservableProperty]
    private IDataProvider? _dataItems;

    public ResultSetViewModel(QueryResult result, int index)
    {
        _columnNames = result.ColumnNames;
        _rows = result.Rows;
        HasError = !result.IsSuccess;

        if (result.IsSuccess)
        {
            Title = $"Result {index}: {result.RowCount} row(s)";
            Status = $"{result.RowCount} row(s) in {result.ExecutionTimeMs}ms";
            LoadColumns();
        }
        else
        {
            Title = $"Result {index}: Error";
            Status = $"Error: {result.ErrorMessage}";
        }
    }

    private void LoadColumns()
    {
        if (_columnNames.Count == 0) return;

        foreach (var colName in _columnNames)
        {
            Columns.Add(new VirtualDataGridTextColumn(colName, row =>
            {
                if (row is Dictionary<string, object?> dict && dict.TryGetValue(colName, out var value))
                    return value;
                return null;
            }));
        }
        DataItems = new InMemoryDataProvider<Dictionary<string, object?>>(_rows);
    }

    [RelayCommand]
    private async Task ExportCsv()
    {
        if (_rows.Count == 0) return;

        var storage = GetStorageProvider();
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as CSV",
            SuggestedFileName = "export.csv",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("CSV File") { Patterns = new[] { "*.csv" } }
            }
        });

        if (file == null) return;

        var csv = new StringBuilder();
        csv.AppendLine(string.Join(",", _columnNames.Select(EscapeCsv)));

        foreach (var row in _rows)
        {
            var values = _columnNames.Select(col =>
            {
                var value = row.TryGetValue(col, out var v) ? v : null;
                return EscapeCsv(value?.ToString() ?? "");
            });
            csv.AppendLine(string.Join(",", values));
        }

        await File.WriteAllTextAsync(file.Path.LocalPath, csv.ToString());
    }

    [RelayCommand]
    private async Task ExportJson()
    {
        if (_rows.Count == 0) return;

        var storage = GetStorageProvider();
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Export as JSON",
            SuggestedFileName = "export.json",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("JSON File") { Patterns = new[] { "*.json" } }
            }
        });

        if (file == null) return;

        var exportData = _rows.Select(row =>
        {
            var dict = new Dictionary<string, object?>();
            foreach (var col in _columnNames)
            {
                dict[col] = row.TryGetValue(col, out var value) ? value : null;
            }
            return dict;
        }).ToList();

        var json = JsonSerializer.Serialize(exportData, new JsonSerializerOptions
        {
            WriteIndented = true
        });

        await File.WriteAllTextAsync(file.Path.LocalPath, json);
    }

    private static string EscapeCsv(string value)
    {
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n') || value.Contains('\r'))
        {
            return $"\"{value.Replace("\"", "\"\"")}\"";
        }
        return value;
    }

    private static IStorageProvider? GetStorageProvider()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.StorageProvider
            : null;
    }
}
