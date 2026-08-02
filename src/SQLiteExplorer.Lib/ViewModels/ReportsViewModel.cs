using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.ViewModels;

/// <summary>
/// Backs the reports manager dialog: list saved reports, generate Excel files,
/// and raise events for New/Edit (the host opens the wizard dialog).
/// </summary>
public partial class ReportsViewModel : ViewModelBase
{
    private readonly ReportStore _store;
    private readonly Func<IDatabaseService?> _databaseServiceFactory;

    [ObservableProperty]
    private ObservableCollection<ReportDefinition> _reports = new();

    [ObservableProperty]
    private ReportDefinition? _selectedReport;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _hasError;

    [ObservableProperty]
    private bool _isBusy;

    public ReportsViewModel(ReportStore store, Func<IDatabaseService?> databaseServiceFactory)
    {
        _store = store;
        _databaseServiceFactory = databaseServiceFactory;
        Reload();
    }

    /// <summary>Raised when the user wants to create a new report (open the wizard).</summary>
    public event EventHandler? NewRequested;

    /// <summary>Raised when the user wants to edit a report (open the wizard).</summary>
    public event EventHandler<ReportDefinition>? EditRequested;

    public void Reload()
    {
        Reports = new ObservableCollection<ReportDefinition>(
            _store.Load().OrderByDescending(r => r.ModifiedUtc));
    }

    [RelayCommand]
    private void New()
    {
        NewRequested?.Invoke(this, EventArgs.Empty);
    }

    [RelayCommand]
    private void Edit()
    {
        if (SelectedReport != null)
        {
            EditRequested?.Invoke(this, SelectedReport);
        }
    }

    [RelayCommand]
    private void Delete()
    {
        if (SelectedReport == null) return;

        _store.Delete(SelectedReport.Id);
        SelectedReport = null;
        Reload();
    }

    [RelayCommand]
    private async Task Generate()
    {
        if (SelectedReport == null) return;

        var db = _databaseServiceFactory();
        if (db == null || !db.IsConnected)
        {
            SetError("No database connected");
            return;
        }

        var storage = GetStorageProvider();
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Save Excel Report",
            SuggestedFileName = $"{SanitizeFileName(SelectedReport.Name)}.xlsx",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("Excel Workbook") { Patterns = new[] { "*.xlsx" } }
            }
        });

        if (file == null) return;

        IsBusy = true;
        HasError = false;
        StatusMessage = string.Empty;

        try
        {
            var multiResult = await db.ExecuteMultipleAsync(SelectedReport.Sql);
            var result = multiResult.Results.FirstOrDefault(r => r.IsSuccess);

            if (result == null)
            {
                SetError(multiResult.FirstError ?? "Query failed");
                return;
            }

            ExcelReportWriter.Write(result, SelectedReport, file.Path.LocalPath);
            StatusMessage = $"Report saved: {Path.GetFileName(file.Path.LocalPath)} ({result.RowCount} rows)";
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

    private void SetError(string message)
    {
        StatusMessage = message;
        HasError = true;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var sanitized = new string(chars).Trim();
        return string.IsNullOrEmpty(sanitized) ? "report" : sanitized;
    }

    private static IStorageProvider? GetStorageProvider()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.StorageProvider
            : null;
    }
}
