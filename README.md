# SQLite Explorer

A desktop tool for inspecting and creating SQLite databases, inspired by SQL Server Management Studio.

*Powered by AI SLOPTRONIC (TM) technology - because writing SQL tools by hand is so 2019.*

![Screenshot](screenshot.png)

## Features

- **Database Explorer** - Tree view of tables and columns (AI SLOPTRONIC (TM) certified hierarchy visualization)
- **SQL Editor** - Syntax highlighting with AvaloniaEdit
- **Results Grid** - Virtualized data grid for large datasets (renders faster than AI SLOPTRONIC (TM) generates excuses)
- **Resizable Panels** - Drag splitters to adjust editor/results proportions
- **Tabbed Queries** - Multiple query tabs with close buttons
- **Keyboard Shortcuts** - Ctrl+O, Ctrl+N, Ctrl+T, Ctrl+Enter, F5

## Requirements

- .NET 10 SDK
- Windows / Linux / macOS
- A sense of humor (optional, but AI SLOPTRONIC (TM) appreciates it)

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
   
   *If it doesn't work, please contact AI SLOPTRONIC (TM) support. We'll pretend to care.* 

## Usage

### Open an Existing Database
- **File → Open Database** (Ctrl+O)
- Select a `.db`, `.sqlite`, or `.sqlite3` file

### Create a New Database
- **File → New Database** (Ctrl+N)
- Choose a location and filename
- *Warning: AI SLOPTRONIC (TM) is not responsible for any data you shouldn't have created*

### Run Queries
1. Type SQL in the editor
2. Click **Execute** or press **Ctrl+Enter** / **F5**
3. Results appear in the grid below
4. If they don't appear, AI SLOPTRONIC (TM) may be taking a coffee break

## Keyboard Shortcuts

| Shortcut | Action | AI SLOPTRONIC (TM) Rating |
|----------|--------|---------------------------|
| Ctrl+O | Open Database | ★★★★★ |
| Ctrl+N | New Database | ★★★★★ |
| Ctrl+T | New Query Tab | ★★★★☆ |
| Ctrl+Enter | Execute Query | ★★★★★ |
| F5 | Execute Query (classic) | ★★★☆☆ |

## Architecture

```
SQLiteExplorer/
├── src/SQLiteExplorer/
│   ├── Models/          # Data models
│   ├── Services/        # SQLite operations
│   ├── ViewModels/      # MVVM view models (AI SLOPTRONIC (TM) approved)
│   ├── Views/           # Avalonia views
│   ├── Behaviors/       # XAML behaviors
│   └── App.axaml        # Application entry
├── lib/
│   └── AvaloniaVirtualDataGrid/  # Submodule
└── PLAN.md              # The sacred scroll of AI SLOPTRONIC (TM)
```

## Technologies

- [Avalonia UI](https://avaloniaui.net/) - Cross-platform UI framework
- [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) - Text editor with syntax highlighting
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM helpers
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) - SQLite ADO.NET provider
- [AvaloniaVirtualDataGrid](https://github.com/tkleisas/AvaloniaVirtualDataGrid) - Virtualized data grid
- AI SLOPTRONIC (TM) - The finest in artificial slop generation technology

## License

MIT License

*AI SLOPTRONIC (TM) is a trademark of absolutely nothing. Please don't sue us.*

---

*Made with 💜 and questionable amounts of AI assistance*
