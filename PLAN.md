# Database Explorer - Architecture Plan

A cross-platform desktop tool for inspecting and querying SQLite, PostgreSQL, SQL Server
and Oracle databases, inspired by SQL Server Management Studio.

## Project Structure
```
SQLiteExplorer/
├── SQLiteExplorer.sln
├── src/
│   ├── SQLiteExplorer/                          # Desktop app shell
│   │   ├── App.axaml(.cs)                       # Application entry
│   │   └── Views/                               # MainWindow, AboutWindow
│   └── SQLiteExplorer.Lib/                      # Reusable library
│       ├── ViewModels/
│       │   ├── MainWindowViewModel.cs           # Main window logic + tree
│       │   └── QueryTabViewModel.cs             # Query tab + results
│       ├── Views/                               # Controls + connection dialogs
│       ├── Models/                              # ConnectionInfo (+ per-dialect), TableInfo, ...
│       └── Services/
│           ├── IDatabaseService.cs              # Provider abstraction (dialect-aware)
│           ├── SqliteService.cs                 # SQLite
│           ├── PostgresService.cs               # PostgreSQL
│           ├── SqlServerService.cs              # SQL Server
│           ├── OracleService.cs                 # Oracle (11g R2+)
│           ├── SqlStatementSplitter.cs          # Shared statement splitter
│           └── DatabaseServiceFactory.cs        # Provider factory
├── tests/
│   └── SQLiteExplorer.Lib.Tests/                # xUnit tests
├── lib/
│   └── AvaloniaVirtualDataGrid/                # Git submodule
└── .github/workflows/                          # ci.yml, release.yml
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
| `Avalonia` (12) | UI framework |
| `Avalonia.AvaloniaEdit` | SQL editor with syntax highlighting |
| `CommunityToolkit.Mvvm` | MVVM helpers |
| `Microsoft.Data.Sqlite` | SQLite ADO.NET provider |
| `Npgsql` | PostgreSQL ADO.NET provider |
| `Microsoft.Data.SqlClient` | SQL Server ADO.NET provider |
| `Oracle.ManagedDataAccess.Core` | Oracle ADO.NET provider (ODP.NET Core 19c line, DB 11g R2+) |
| `AvaloniaVirtualDataGrid` | Virtualized data grid |
| `xunit` | Unit testing |

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

### Phase 6: Multi-Database, Tests & CI ✅
- [x] Extract reusable `SQLiteExplorer.Lib` class library
- [x] Provider abstraction (`IDatabaseService`) with dialect-aware quoting/describe
- [x] PostgreSQL, SQL Server and Oracle providers (Oracle 11g R2+)
- [x] Schema-aware object tree (Database > Schema > Table > Column) for server databases
- [x] Upgrade to Avalonia 12
- [x] xUnit test project (`tests/SQLiteExplorer.Lib.Tests`)
- [x] GitHub Actions: CI (build + test) and tag-triggered release builds

### Phase 7: LLM Integration ✅
- [x] `ILlmService` abstraction + built-in OpenAI-compatible service (no new deps)
- [x] Host injection: embedders (NVS) assign `MainWindowViewModel.LlmService` to reuse
      the host's LLM setup; `LlmSettingsRequested` routes the settings button to the host
- [x] AI Assistant panel: natural-language → SQL, Explain, Optimize, Analyze Results
- [x] AI completion at caret (Ctrl+Shift+Space), schema/dialect-aware prompts
- [x] AI Settings dialog (endpoint, key, model, temperature, test connection)
- [x] xUnit tests for prompts, the LLM client and the assistant view model

### Phase 8: UI Polish ✅
- [x] Toolbar: connection chip, accent Execute, AI toggle, history on the right
- [x] Status bar: connection dot, db-type badge, proper error coloring
- [x] Consistent button styles (tool/accent/chip/flat), rounded editor chrome
- [x] AI side panel mirroring the cheatsheet panel idiom

### Phase 9: Reports ✅
- [x] `ReportDefinition` + JSON `ReportStore` (AppData default; hosts can redirect per-workspace)
- [x] Report wizard: details → SQL (hand-written or AI-generated) → preview → save
- [x] Reports manager: list, edit, delete, generate
- [x] Excel output via ClosedXML (title block, frozen bold header, typed cells, auto-fit)
- [x] `IsMenuVisible` for hosts that surface commands in their own menus (NVS)

## Keyboard Shortcuts
| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open Database |
| Ctrl+N | New Database |
| Ctrl+T | New Query Tab |
| Ctrl+Enter | Execute Query |
| Ctrl+Shift+Space | AI Completion at Caret |
| F5 | Execute Query |

## Known Issues
- File dialogs use deprecated API (should update to StorageProvider)

## Lessons Learned
- AvaloniaEdit requires its theme to be included: `<StyleInclude Source="avares://AvaloniaEdit/Themes/Fluent/AvaloniaEdit.xaml" />`
- Use `{ReflectionBinding}` for behavior bindings when `AvaloniaUseCompiledBindingsByDefault=true`
- TextEditor needs `DocumentTextBindingBehavior` for MVVM text binding

## Future Enhancements
- Inline data editing
- Streaming LLM responses in the AI panel
- LLM-generated query explanations inline in result tabs
- Table creation UI
- Index management

## Technical Decisions
- **.NET Version**: .NET 10
- **UI Framework**: Avalonia 12
- **MVVM Framework**: CommunityToolkit.Mvvm
- **SQL Editor**: AvaloniaEdit (SQL syntax highlighting)
- **Data Editing**: Read-only
- **Data Grid**: AvaloniaVirtualDataGrid
- **Databases**: SQLite, PostgreSQL, SQL Server, Oracle (provider-based)
