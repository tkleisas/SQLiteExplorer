using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data.Converters;
using Avalonia.Platform.Storage;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using SQLiteExplorer.Completion;
using SQLiteExplorer.Models;
using SQLiteExplorer.Services;

namespace SQLiteExplorer.ViewModels;

public partial class MainWindowViewModel : ViewModelBase
{
    private readonly ISqliteService _sqliteService;
    
    [ObservableProperty]
    private string _title = "SQLite Explorer";
    
    [ObservableProperty]
    private string _statusMessage = "No database loaded";
    
    [ObservableProperty]
    private bool _isConnected;
    
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

    public SqlCompletionProvider CompletionProvider { get; } = new();

    public static IValueConverter ConnectedConverter { get; } = new FuncValueConverter<bool, string>(b => b ? "Connected" : "Disconnected");

    public MainWindowViewModel() : this(new SqliteService()) { }

    public MainWindowViewModel(ISqliteService sqliteService)
    {
        _sqliteService = sqliteService;
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
    private async Task OpenDatabase()
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
        await LoadDatabaseAsync(path);
    }

    [RelayCommand]
    private async Task NewDatabase()
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
        await LoadDatabaseAsync(path);
    }

    private async Task LoadDatabaseAsync(string path)
    {
        try
        {
            await _sqliteService.OpenDatabaseAsync(path);
            IsConnected = _sqliteService.IsConnected;
            StatusMessage = $"Connected: {Path.GetFileName(path)}";
            Title = $"SQLite Explorer - {Path.GetFileName(path)}";
            
            await RefreshDatabaseTree();
        }
        catch (Exception ex)
        {
            StatusMessage = $"Error: {ex.Message}";
        }
    }

    [RelayCommand]
    private async Task RefreshDatabaseTree()
    {
        if (!_sqliteService.IsConnected) return;

        DatabaseNodes.Clear();
        
        var info = await _sqliteService.GetDatabaseInfoAsync();
        var rootNode = new DatabaseTreeNode
        {
            Name = info.Name,
            NodeType = NodeType.Database,
            Path = info.Path,
            IsExpanded = true
        };

        var tableNames = new List<string>();
        var tableColumns = new Dictionary<string, List<string>>();

        foreach (var table in info.Tables)
        {
            var tableNode = new DatabaseTreeNode
            {
                Name = table.Name,
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

            rootNode.Children.Add(tableNode);
        }

        DatabaseNodes.Add(rootNode);
        CompletionProvider.UpdateSchema(tableNames, tableColumns);
    }

    [RelayCommand]
    private void AddNewQueryTab()
    {
        var tab = new QueryTabViewModel(_sqliteService)
        {
            Title = $"Query {QueryTabs.Count + 1}"
        };
        tab.QueryExecuted += (_, sql) => AddToHistory(sql);
        QueryTabs.Add(tab);
        SelectedTab = tab;
    }

    [RelayCommand]
    private void GenerateSelect(string tableName)
    {
        if (SelectedTab == null)
        {
            AddNewQueryTab();
        }
        
        if (SelectedTab != null)
        {
            SelectedTab.SqlText = $"SELECT * FROM {tableName};";
        }
    }

    [RelayCommand]
    private void SelectTable(DatabaseTreeNode node)
    {
        if (node == null || !node.IsTableOrView) return;
        GenerateSelect(node.Name);
    }

    [RelayCommand]
    private void DescribeTable(DatabaseTreeNode node)
    {
        if (node == null || !node.IsTableOrView) return;
        
        if (SelectedTab == null)
        {
            AddNewQueryTab();
        }
        
        if (SelectedTab != null)
        {
            SelectedTab.SqlText = $"PRAGMA table_info({node.Name});";
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
        var aboutWindow = new Views.AboutWindow();
        
        if (Avalonia.Application.Current?.ApplicationLifetime is Avalonia.Controls.ApplicationLifetimes.IClassicDesktopStyleApplicationLifetime desktop 
            && desktop.MainWindow != null)
        {
            aboutWindow.ShowDialog(desktop.MainWindow);
        }
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
}

public enum NodeType
{
    Database,
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
    private ObservableCollection<DatabaseTreeNode> _children = new();

    [ObservableProperty]
    private bool _isExpanded;

    public string Icon => NodeType switch
    {
        NodeType.Database => "🗄️",
        NodeType.Table => "📋",
        NodeType.View => "👁️",
        NodeType.Column => "📝",
        _ => "📄"
    };

    public string DisplayName => $"{Icon} {Name}";

    public bool IsTableOrView => NodeType == NodeType.Table || NodeType == NodeType.View;
}
