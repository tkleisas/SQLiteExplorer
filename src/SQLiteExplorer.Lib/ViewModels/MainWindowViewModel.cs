using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Lib.Completion;
using SQLiteExplorer.Lib.Models;
using SQLiteExplorer.Lib.Services;

namespace SQLiteExplorer.Lib.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private IDatabaseService? _databaseService;
    private string _schemaDescription = string.Empty;
    private ILlmService _llmService;

    [ObservableProperty]
    private string _title = "SQLite Explorer";

    [ObservableProperty]
    private string _statusMessage = "No database loaded";

    [ObservableProperty]
    private bool _isConnected;

    [ObservableProperty]
    private string _connectionDisplayName = string.Empty;

    [ObservableProperty]
    private DatabaseType _currentDatabaseType;

    [ObservableProperty]
    private bool _isLlmHostConfigured;

    /// <summary>
    /// Whether the embedded menu bar is shown. Hosts that integrate the explorer's
    /// commands into their own menu system (e.g. NVS) set this to false.
    /// </summary>
    [ObservableProperty]
    private bool _isMenuVisible = true;

    private string? _reportStoreDirectory;
    private ReportStore? _reportStore;

    /// <summary>
    /// Directory where report definitions are stored. Null (default) means
    /// %APPDATA%/SQLiteExplorer. Hosts can set a per-workspace directory.
    /// </summary>
    public string? ReportStoreDirectory
    {
        get => _reportStoreDirectory;
        set
        {
            if (SetProperty(ref _reportStoreDirectory, value))
            {
                _reportStore = null;
            }
        }
    }

    private ReportStore GetReportStore() => _reportStore ??= new ReportStore(_reportStoreDirectory);

    [ObservableProperty]
    private ObservableCollection<DatabaseTreeNode> _databaseNodes = new();

    [ObservableProperty]
    private ObservableCollection<QueryTabViewModel> _queryTabs = new();

    [ObservableProperty]
    private QueryTabViewModel? _selectedTab;

    [ObservableProperty]
    private ObservableCollection<string> _queryHistory = new();

    [ObservableProperty]
    private string? _selectedHistoryItem;

    public CheatsheetViewModel Cheatsheet { get; } = new();

    public SqlCompletionProvider CompletionProvider { get; } = new();

    public AiAssistantViewModel AiAssistant { get; }

    /// <summary>
    /// The LLM service used for AI completion and analysis. Defaults to the built-in
    /// OpenAI-compatible service configured via the AI Settings dialog. Hosts embedding
    /// the explorer can assign their own implementation to reuse the host's LLM setup;
    /// doing so marks the configuration as host-managed (see <see cref="IsLlmHostConfigured"/>).
    /// </summary>
    public ILlmService LlmService
    {
        get => _llmService;
        set
        {
            if (SetProperty(ref _llmService, value))
            {
                IsLlmHostConfigured = value is not OpenAiCompatibleLlmService;
            }
        }
    }

    public static IValueConverter ConnectedConverter { get; } = new FuncValueConverter<bool, string>(b => b ? "Connected" : "Disconnected");

    public static IValueConverter ConnectedBrushConverter { get; } = new FuncValueConverter<bool, Avalonia.Media.IBrush>(b => b ? Avalonia.Media.Brushes.ForestGreen : Avalonia.Media.Brushes.Gray);

    public MainWindowViewModel()
    {
        _llmService = new OpenAiCompatibleLlmService(LlmSettings.Load());
        AiAssistant = new AiAssistantViewModel(() => LlmService, () => _schemaDescription);
        AddNewQueryTab();
    }

    partial void OnSelectedHistoryItemChanged(string? value)
    {
        if (value != null && SelectedTab != null)
        {
            SelectedTab.SqlText = value;
        }
    }

    public void AddToHistory(string sql)
    {
        if (string.IsNullOrWhiteSpace(sql)) return;
        
        var trimmed = sql.Trim();
        if (QueryHistory.Contains(trimmed))
        {
            QueryHistory.Remove(trimmed);
        }
        QueryHistory.Insert(0, trimmed);
        
        if (QueryHistory.Count > 50)
        {
            QueryHistory.RemoveAt(QueryHistory.Count - 1);
        }
    }

    [RelayCommand]
    private async Task OpenSqliteDatabase()
    {
        var storage = GetStorageProvider();
        if (storage == null) return;

        var files = await storage.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open SQLite Database",
            AllowMultiple = false,
            FileTypeFilter = new[]
            {
                new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" } },
                new FilePickerFileType("All Files") { Patterns = new[] { "*.*" } }
            }
        });

        if (files.Count == 0) return;

        var path = files[0].Path.LocalPath;
        var connectionInfo = new SqliteConnectionInfo { FilePath = path };
        await ConnectDatabaseAsync(connectionInfo);
    }

    [RelayCommand]
    private async Task NewSqliteDatabase()
    {
        var storage = GetStorageProvider();
        if (storage == null) return;

        var file = await storage.SaveFilePickerAsync(new FilePickerSaveOptions
        {
            Title = "Create New SQLite Database",
            SuggestedFileName = "database.db",
            FileTypeChoices = new[]
            {
                new FilePickerFileType("SQLite Database") { Patterns = new[] { "*.db", "*.sqlite", "*.sqlite3" } }
            }
        });

        if (file == null) return;

        var path = file.Path.LocalPath;
        var connectionInfo = new SqliteConnectionInfo { FilePath = path };
        await ConnectDatabaseAsync(connectionInfo);
    }

    [RelayCommand]
    private async Task OpenPostgresDatabase()
    {
        var dialog = new Views.PostgresConnectionDialog();
        
        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }

        if (dialog.ConnectionInfo != null)
        {
            await ConnectDatabaseAsync(dialog.ConnectionInfo);
        }
    }

    [RelayCommand]
    private async Task OpenSqlServerDatabase()
    {
        var dialog = new Views.SqlServerConnectionDialog();

        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }

        if (dialog.ConnectionInfo != null)
        {
            await ConnectDatabaseAsync(dialog.ConnectionInfo);
        }
    }

    [RelayCommand]
    private async Task OpenOracleDatabase()
    {
        var dialog = new Views.OracleConnectionDialog();

        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }

        if (dialog.ConnectionInfo != null)
        {
            await ConnectDatabaseAsync(dialog.ConnectionInfo);
        }
    }

    /// <summary>
    /// Opens a SQLite database file by path without showing a file picker dialog.
    /// </summary>
    public async Task OpenDatabaseByPathAsync(string filePath)
    {
        var connectionInfo = new SqliteConnectionInfo { FilePath = filePath };
        await ConnectDatabaseAsync(connectionInfo);
    }

    /// <summary>
    /// Executes a SQL query in a new query tab. Requires an open database connection.
    /// </summary>
    public async Task ExecuteSqlAsync(string sql)
    {
        if (!IsConnected || string.IsNullOrWhiteSpace(sql)) return;

        AddNewQueryTab();
        if (SelectedTab is null) return;

        SelectedTab.SqlText = sql;
        await SelectedTab.ExecuteQueryCommand.ExecuteAsync(null);
    }

    private async Task ConnectDatabaseAsync(ConnectionInfo connectionInfo)
    {
        try
        {
            _databaseService?.Dispose();
            _databaseService = DatabaseServiceFactory.Create(connectionInfo.DatabaseType);
            
            var success = await _databaseService.ConnectAsync(connectionInfo);
            if (!success)
            {
                StatusMessage = "Failed to connect";
                return;
            }

            CurrentDatabaseType = connectionInfo.DatabaseType;
            IsConnected = _databaseService.IsConnected;
            StatusMessage = $"Connected: {connectionInfo.DisplayName}";
            ConnectionDisplayName = connectionInfo.DisplayName;
            Title = $"{GetAppTitle()} - {connectionInfo.DisplayName}";
            
            await RefreshDatabaseTree();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    private string GetAppTitle()
    {
        return CurrentDatabaseType switch
        {
            DatabaseType.PostgreSQL => "PostgreSQL Explorer",
            DatabaseType.SqlServer => "SQL Server Explorer",
            DatabaseType.Oracle => "Oracle Explorer",
            _ => "SQLite Explorer"
        };
    }

    [RelayCommand]
    private async Task RefreshDatabaseTree()
    {
        if (_databaseService == null || !_databaseService.IsConnected) return;

        DatabaseNodes.Clear();
        
        var info = await _databaseService.GetDatabaseInfoAsync();
        var rootNode = new DatabaseTreeNode
        {
            Name = info.Name,
            NodeType = NodeType.Database,
            Path = info.Path,
            IsExpanded = true
        };

        var tableNames = new List<string>();
        var tableColumns = new Dictionary<string, List<string>>();

        if (_databaseService.UsesSchemas)
        {
            foreach (var schemaGroup in info.Tables
                         .GroupBy(t => t.Schema)
                         .OrderBy(g => g.Key, StringComparer.OrdinalIgnoreCase))
            {
                var schemaNode = new DatabaseTreeNode
                {
                    Name = schemaGroup.Key,
                    Schema = schemaGroup.Key,
                    NodeType = NodeType.Schema,
                    IsExpanded = false
                };

                foreach (var table in schemaGroup)
                {
                    schemaNode.Children.Add(BuildTableNode(table, tableNames, tableColumns));
                }

                rootNode.Children.Add(schemaNode);
            }
        }
        else
        {
            foreach (var table in info.Tables)
            {
                rootNode.Children.Add(BuildTableNode(table, tableNames, tableColumns));
            }
        }

        DatabaseNodes.Add(rootNode);
        CompletionProvider.UpdateSchema(tableNames, tableColumns);
        _schemaDescription = LlmPrompts.BuildSchemaDescription(
            GetDialectDisplayName(),
            info.Tables);
    }

    private string GetDialectDisplayName()
    {
        return CurrentDatabaseType switch
        {
            DatabaseType.PostgreSQL => "PostgreSQL",
            DatabaseType.SqlServer => "SQL Server (T-SQL)",
            DatabaseType.Oracle => "Oracle (PL/SQL)",
            _ => "SQLite"
        };
    }

    private static DatabaseTreeNode BuildTableNode(
        TableInfo table,
        List<string> tableNames,
        Dictionary<string, List<string>> tableColumns)
    {
        var tableNode = new DatabaseTreeNode
        {
            Name = table.Name,
            Schema = table.Schema,
            NodeType = table.Type == "view" ? NodeType.View : NodeType.Table,
            IsExpanded = false
        };

        tableNames.Add(table.Name);
        tableColumns[table.Name] = table.Columns.Select(c => c.Name).ToList();

        foreach (var column in table.Columns)
        {
            tableNode.Children.Add(new DatabaseTreeNode
            {
                Name = $"{column.Name} ({column.Type})",
                NodeType = NodeType.Column
            });
        }

        return tableNode;
    }

    [RelayCommand]
    private void AddNewQueryTab()
    {
        var tab = new QueryTabViewModel(() => _databaseService);
        tab.Title = $"Query {QueryTabs.Count + 1}";
        tab.QueryExecuted += (_, sql) => AddToHistory(sql);
        QueryTabs.Add(tab);
        SelectedTab = tab;
    }

    [RelayCommand]
    private void GenerateSelect(string tableName)
    {
        WriteSelectInto(null, tableName);
    }

    [RelayCommand]
    private void SelectTable(DatabaseTreeNode node)
    {
        if (node == null || !node.IsTableOrView) return;
        WriteSelectInto(node.Schema, node.Name);
    }

    private void WriteSelectInto(string? schema, string tableName)
    {
        if (SelectedTab == null)
        {
            AddNewQueryTab();
        }

        if (SelectedTab != null)
        {
            var quotedName = _databaseService?.QuoteIdentifier(schema, tableName) ?? tableName;
            SelectedTab.SqlText = $"SELECT * FROM {quotedName};";
        }
    }

    [RelayCommand]
    private void DescribeTable(DatabaseTreeNode node)
    {
        if (node == null || !node.IsTableOrView) return;

        if (SelectedTab == null)
        {
            AddNewQueryTab();
        }

        if (SelectedTab != null && _databaseService != null)
        {
            SelectedTab.SqlText = _databaseService.GetDescribeSql(node.Schema, node.Name);
        }
    }

    [RelayCommand]
    private void CloseQueryTab(QueryTabViewModel tab)
    {
        QueryTabs.Remove(tab);
        if (SelectedTab == tab && QueryTabs.Count > 0)
        {
            SelectedTab = QueryTabs[^1];
        }
    }

    [RelayCommand]
    private void ShowAbout()
    {
        // ShowAbout is app-specific and will be handled by the host application.
        // This raises the ShowAboutRequested event for the host to handle.
        ShowAboutRequested?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Raised when the ViewModel requests the About dialog to be shown.
    /// The host application should subscribe to this event and show its own About window.
    /// </summary>
    public event EventHandler? ShowAboutRequested;

    /// <summary>
    /// Raised when the user opens AI settings while the LLM service is host-managed
    /// (<see cref="IsLlmHostConfigured"/>). The host should open its own LLM settings UI.
    /// </summary>
    public event EventHandler? LlmSettingsRequested;

    [RelayCommand]
    private void ShowAiAssistant()
    {
        AiAssistant.IsVisible = true;
    }

    [RelayCommand]
    private void CloseAiAssistant()
    {
        AiAssistant.IsVisible = false;
    }

    [RelayCommand]
    private async Task OpenLlmSettings()
    {
        if (IsLlmHostConfigured)
        {
            LlmSettingsRequested?.Invoke(this, EventArgs.Empty);
            return;
        }

        var dialog = new Views.LlmSettingsDialog();

        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }

        if (dialog.SettingsSaved)
        {
            ReloadLlmService();
        }
    }

    /// <summary>Reloads the built-in LLM service from saved settings (no-op when host-managed).</summary>
    public void ReloadLlmService()
    {
        if (!IsLlmHostConfigured)
        {
            LlmService = new OpenAiCompatibleLlmService(LlmSettings.Load());
        }
    }

    [RelayCommand]
    private async Task ExplainQuery()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.SqlText)) return;

        AiAssistant.IsVisible = true;
        await AiAssistant.ExplainAsync(SelectedTab.SqlText);
    }

    [RelayCommand]
    private async Task OptimizeQuery()
    {
        if (SelectedTab == null || string.IsNullOrWhiteSpace(SelectedTab.SqlText)) return;

        AiAssistant.IsVisible = true;
        await AiAssistant.OptimizeAsync(SelectedTab.SqlText);
    }

    [RelayCommand]
    private async Task AnalyzeResults()
    {
        var resultSet = SelectedTab?.SelectedResultSet;
        if (SelectedTab == null || resultSet == null || resultSet.Rows.Count == 0) return;

        AiAssistant.IsVisible = true;
        await AiAssistant.AnalyzeAsync(SelectedTab.SqlText, resultSet.ColumnNames, resultSet.Rows);
    }

    private bool _isAiCompleting;

    [RelayCommand]
    private async Task AiComplete()
    {
        if (SelectedTab == null || _isAiCompleting) return;

        if (!LlmService.IsConfigured)
        {
            StatusMessage = "LLM is not configured - open AI Settings";
            AiAssistant.IsVisible = true;
            return;
        }

        var beforeCaret = SelectedTab.Editor?.GetTextBeforeCaret() ?? SelectedTab.SqlText;
        if (string.IsNullOrWhiteSpace(beforeCaret)) return;

        _isAiCompleting = true;
        try
        {
            var (system, user) = LlmPrompts.BuildCompletion(_schemaDescription, beforeCaret);
            var completion = await LlmService.ChatAsync(system, user);
            if (!string.IsNullOrWhiteSpace(completion))
            {
                SelectedTab.InsertSqlAtCaret(completion);
            }
        }
        catch (Exception ex)
        {
            StatusMessage = $"AI completion failed: {ex.Message}";
        }
        finally
        {
            _isAiCompleting = false;
        }
    }

    [RelayCommand]
    private void InsertAiSql()
    {
        if (string.IsNullOrWhiteSpace(AiAssistant.SqlFromResponse)) return;

        if (SelectedTab == null)
        {
            AddNewQueryTab();
        }

        SelectedTab?.InsertSqlAtCaret(AiAssistant.SqlFromResponse);
    }

    [RelayCommand]
    private async Task OpenReports()
    {
        if (!IsConnected)
        {
            StatusMessage = "Connect to a database first";
            return;
        }

        var vm = new ReportsViewModel(GetReportStore(), () => _databaseService);
        vm.NewRequested += async (_, _) =>
        {
            await ShowReportWizard(null);
            vm.Reload();
        };
        vm.EditRequested += async (_, report) =>
        {
            await ShowReportWizard(report);
            vm.Reload();
        };

        var dialog = new Views.ReportsDialog(vm);
        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }
    }

    [RelayCommand]
    private async Task NewReport()
    {
        if (!IsConnected)
        {
            StatusMessage = "Connect to a database first";
            return;
        }

        await ShowReportWizard(null);
    }

    private async Task ShowReportWizard(ReportDefinition? existing)
    {
        var vm = new ReportWizardViewModel(
            () => LlmService,
            () => _schemaDescription,
            () => _databaseService,
            GetReportStore(),
            existing);

        var dialog = new Views.ReportWizardDialog(vm);
        var window = GetMainWindow();
        if (window != null)
        {
            await dialog.ShowDialog(window);
        }
    }

    [RelayCommand]
    private void ShowSqliteCheatsheet()
    {
        Cheatsheet.Title = "SQLite Cheatsheet";
        Cheatsheet.Content = Cheatsheets.Sqlite;
        Cheatsheet.IsVisible = true;
    }

    [RelayCommand]
    private void ShowPostgresCheatsheet()
    {
        Cheatsheet.Title = "PostgreSQL Cheatsheet";
        Cheatsheet.Content = Cheatsheets.Postgres;
        Cheatsheet.IsVisible = true;
    }

    [RelayCommand]
    private void ShowSqlServerCheatsheet()
    {
        Cheatsheet.Title = "SQL Server Cheatsheet";
        Cheatsheet.Content = Cheatsheets.SqlServer;
        Cheatsheet.IsVisible = true;
    }

    [RelayCommand]
    private void ShowOracleCheatsheet()
    {
        Cheatsheet.Title = "Oracle Cheatsheet";
        Cheatsheet.Content = Cheatsheets.Oracle;
        Cheatsheet.IsVisible = true;
    }

    [RelayCommand]
    private void CloseCheatsheet()
    {
        Cheatsheet.IsVisible = false;
    }

    [RelayCommand]
    private void Exit()
    {
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    private static IStorageProvider? GetStorageProvider()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow?.StorageProvider
            : null;
    }

    private static Window? GetMainWindow()
    {
        return Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop
            ? desktop.MainWindow
            : null;
    }
}

public enum NodeType
{
    Database,
    Schema,
    Table,
    View,
    Column
}

public partial class DatabaseTreeNode : ObservableObject
{
    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private NodeType _nodeType;

    [ObservableProperty]
    private string _path = string.Empty;

    [ObservableProperty]
    private string _schema = string.Empty;

    [ObservableProperty]
    private ObservableCollection<DatabaseTreeNode> _children = new();

    [ObservableProperty]
    private bool _isExpanded;

    public string Icon => NodeType switch
    {
        NodeType.Database => "🗄️",
        NodeType.Schema => "📁",
        NodeType.Table => "📋",
        NodeType.View => "👁️",
        NodeType.Column => "📝",
        _ => "📄"
    };

    public string DisplayName => $"{Icon} {Name}";

    public bool IsTableOrView => NodeType == NodeType.Table || NodeType == NodeType.View;
}
