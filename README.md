# SQLite Explorer

A desktop tool for inspecting and creating SQLite databases, inspired by SQL Server Management Studio.

![Screenshot](screenshot.png)

## Features

- **Database Explorer** - Tree view of tables and columns
- **SQL Editor** - Syntax highlighting with AvaloniaEdit
- **Results Grid** - Virtualized data grid for large datasets
- **Resizable Panels** - Drag splitters to adjust editor/results proportions
- **Tabbed Queries** - Multiple query tabs with close buttons
- **Keyboard Shortcuts** - Ctrl+O, Ctrl+N, Ctrl+T, Ctrl+Enter, F5

## Requirements

- .NET 10 SDK
- Windows / Linux / macOS

## Getting Started

1. Clone the repository:
   ```bash
   git clone https://github.com/tkleisas/SQLiteExplorer.git
   cd SQLiteExplorer
   ```

2. Initialize submodules:
   ```bash
   git submodule update --init --recursive
   ```

3. Build and run:
   ```bash
   dotnet build src/SQLiteExplorer/SQLiteExplorer.csproj
   dotnet run --project src/SQLiteExplorer/SQLiteExplorer.csproj
   ```

## Usage

### Open an Existing Database
- **File → Open Database** (Ctrl+O)
- Select a `.db`, `.sqlite`, or `.sqlite3` file

### Create a New Database
- **File → New Database** (Ctrl+N)
- Choose a location and filename

### Run Queries
1. Type SQL in the editor
2. Click **Execute** or press **Ctrl+Enter** / **F5**
3. Results appear in the grid below

## Keyboard Shortcuts

| Shortcut | Action |
|----------|--------|
| Ctrl+O | Open Database |
| Ctrl+N | New Database |
| Ctrl+T | New Query Tab |
| Ctrl+Enter | Execute Query |
| F5 | Execute Query |

## Architecture

```
SQLiteExplorer/
├── src/SQLiteExplorer/
│   ├── Models/          # Data models
│   ├── Services/        # SQLite operations
│   ├── ViewModels/      # MVVM view models
│   ├── Views/           # Avalonia views
│   ├── Behaviors/       # XAML behaviors
│   └── App.axaml        # Application entry
├── lib/
│   └── AvaloniaVirtualDataGrid/  # Submodule
└── PLAN.md
```

## Technologies

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) - Text editor with syntax highlighting
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM helpers
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) - SQLite ADO.NET provider
- [AvaloniaVirtualDataGrid](https://github.com/tkleisas/AvaloniaVirtualDataGrid) - Virtualized data grid

## License

MIT License
