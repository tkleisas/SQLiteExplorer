using System;
using System.Collections.Generic;
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
using SQLiteExplorer.Models;
using SQLiteExplorer.Services;

namespace SQLiteExplorer.ViewModels;

public partial class QueryTabViewModel : ViewModelBase
{
    private readonly ISqliteService _sqliteService;

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
    private VirtualDataGridColumnCollection _columns = new();

    [ObservableProperty]
    private IDataProvider? _dataItems;

    private List<string> _columnNames = new();
    private List<Dictionary<string, object?>> _rows = new();

    public event EventHandler<string>? QueryExecuted;

    public QueryTabViewModel(ISqliteService sqliteService)
    {
        _sqliteService = sqliteService;
    }

    [RelayCommand]
    private async Task ExecuteQuery()
    {
        if (string.IsNullOrWhiteSpace(SqlText)) return;
        if (!_sqliteService.IsConnected)
        {
            ResultStatus = "No database connected";
            HasError = true;
            return;
        }

        IsLoading = true;
        HasError = false;

        try
        {
            var result = await _sqliteService.ExecuteQueryAsync(SqlText);

            if (!result.IsSuccess)
            {
                ResultStatus = $"Error: {result.ErrorMessage}";
                HasError = true;
                Columns.Clear();
                DataItems = null;
                _columnNames.Clear();
                _rows.Clear();
            }
            else
            {
                ResultStatus = $"{result.RowCount} row(s) in {result.ExecutionTimeMs}ms";
                LoadResultData(result);
                QueryExecuted?.Invoke(this, SqlText);
            }
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

    private void LoadResultData(QueryResult result)
    {
        Columns.Clear();
        DataItems = null;

        _columnNames = result.ColumnNames;
        _rows = result.Rows;

        if (result.ColumnNames.Count == 0) return;

        foreach (var colName in result.ColumnNames)
        {
            Columns.Add(new VirtualDataGridTextColumn(colName, row =>
            {
                if (row is Dictionary<string, object?> dict && dict.TryGetValue(colName, out var value))
                    return value;
                return null;
            }));
        }
        DataItems = new InMemoryDataProvider<Dictionary<string, object?>>(result.Rows);
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
