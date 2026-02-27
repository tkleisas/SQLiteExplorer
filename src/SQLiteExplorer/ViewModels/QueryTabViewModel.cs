using System;
using System.Collections.Generic;
using System.Threading.Tasks;
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
            }
            else
            {
                ResultStatus = $"{result.RowCount} row(s) in {result.ExecutionTimeMs}ms";
                LoadResultData(result);
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
}
