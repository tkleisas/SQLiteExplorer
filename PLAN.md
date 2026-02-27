# SQLite Explorer - Architecture Plan

A desktop tool for inspecting and creating SQLite databases, inspired by SQL Server Management Studio.

## Project Structure
```
SQLiteExplorer/
├── SQLiteExplorer.sln
├── src/
│   └── SQLiteExplorer/                          # Main application
│       ├── App.axaml                           # Application entry
│       ├── App.axaml.cs
│       ├── ViewModels/
│       │   ├── MainWindowViewModel.cs          # Main window logic + tree
│       │   └── QueryTabViewModel.cs            # Query tab + results
│       ├── Views/
│       │   └── MainWindow.axaml(.cs)           # Main window layout
│       ├── Models/
│       │   ├── DatabaseInfo.cs
│       │   ├── TableInfo.cs
│       │   ├── ColumnInfo.cs
│       │   └── QueryResult.cs
│       └── Services/
│           ├── ISqliteService.cs               # Abstraction for SQLite ops
│           └── SqliteService.cs                # Implementation
└── lib/
    └── AvaloniaVirtualDataGrid/                # Git submodule
```

## UI Layout
```
┌─────────────────────────────────────────────────────────────────┐
│ File | View | Query                                   [_][□][X]│
├────────────────┬────────────────────────────────────────────────┤
│                │  [Query 1 ×][+]                               │
│ Database       │ ┌────────────────────────────────────────────┐│
│  Explorer      │ │ SELECT * FROM Table1                       ││
│                │ │                                            ││
│  📁 mydb.db    │ └────────────────────────────────────────────┘│
│   ├─ 📊 Table1 │ ┌────────────────────────────────────────────┐│
│   │  ├─ id     │ │ Id │ Name    │ Email           │ ...       ││
│   │  └─ name   │ │ 1  │ John    │ john@test.com   │           ││
│   └─ 📊 Table2 │ └────────────────────────────────────────────┘│
│                │ 5 row(s) in 12ms                              │
├────────────────┴────────────────────────────────────────────────┤
│ Connected: mydb.db                                   Connected │
└─────────────────────────────────────────────────────────────────┘
```

## Key Packages
| Package | Purpose |
|---------|---------|
| `Avalonia` | UI framework |
| `Avalonia.AvaloniaEdit` | SQL editor with syntax highlighting |
| `CommunityToolkit.Mvvm` | MVVM helpers |
| `Microsoft.Data.Sqlite` | SQLite ADO.NET provider |
| `AvaloniaVirtualDataGrid` | Virtualized data grid |

## Implementation Status

### Phase 1: Core Shell ✅
- [x] Project setup with .NET 10, Avalonia
- [x] Main window with split layout (left pane + right area)
- [x] Basic MVVM infrastructure with CommunityToolkit.Mvvm

### Phase 2: Database Tree ✅
- [x] TreeView with database/tables/columns hierarchy
- [x] Open database dialog
- [x] New database dialog
- [x] Schema introspection via SQLite APIs

### Phase 3: Query Editor ✅
- [x] Tab management (add/close/switch)
- [x] Execute button
- [x] Keyboard shortcuts (Ctrl+O, Ctrl+N, Ctrl+T, Ctrl+Enter, F5)
- [x] AvaloniaEdit integration with SQL syntax highlighting

### Phase 4: Results Grid ✅
- [x] Integrate AvaloniaVirtualDataGrid
- [x] Map query results to IDataProvider
- [x] Column auto-generation from schema
- [x] Status bar (row count, execution time)

### Phase 5: Polish ✅
- [x] Menu bar (File: Open, New, Exit; View: Refresh; Query: New Tab)
- [x] Keyboard shortcuts
- [x] Status bar with connection status
- [ ] Error styling in status bar

## Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open Database |
| Ctrl+N | New Database |
| Ctrl+T | New Query Tab |
| Ctrl+Enter | Execute Query |
| F5 | Execute Query |

## Known Issues
- File dialogs use deprecated API (should update to StorageProvider)
- No error highlighting in status bar (errors shown in green)

## Lessons Learned
- AvaloniaEdit requires its theme to be included: `<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />`
- Use `{ReflectionBinding}` for behavior bindings when `AvaloniaUseCompiledBindingsByDefault=true`
- TextEditor needs `DocumentTextBindingBehavior` for MVVM text binding

## Future Enhancements
- AvaloniaEdit with SQL syntax highlighting
- Inline data editing
- IntelliSense/auto-complete
- Table creation UI
- Index management
- Query history
- Export (CSV, JSON)

## Technical Decisions
- **.NET Version**: .NET 10
- **MVVM Framework**: CommunityToolkit.Mvvm
- **SQL Editor**: TextBox (AvaloniaEdit pending)
- **Data Editing**: Read-only
- **Data Grid**: AvaloniaVirtualDataGrid
