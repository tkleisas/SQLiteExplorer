# Database Explorer

A cross-platform desktop tool for inspecting and querying **SQLite, PostgreSQL, SQL Server and Oracle** databases, inspired by SQL Server Management Studio.

*Powered by AI SLOPTRONIC (TM) technology - because writing SQL tools by hand is so 2019.*

**Version 1.0.0**

![Screenshot](screenshot.png)

## Features

- **Multi-Database Support** - SQLite, PostgreSQL, SQL Server and Oracle
- **Schema-Aware Explorer** - Tree view with icons (🗄️ Database, 📁 Schema, 📋 Table, 👁️ View, 📝 Column). Server databases group objects by schema; SQLite stays flat.
- **SQL Editor** - Syntax highlighting with AvaloniaEdit
- **SQL Autocomplete** - Keywords, functions, tables, and columns (Ctrl+Space or auto-trigger)
- **Multiple Results** - Execute multiple statements, each result in its own tab (SSMS-style)
- **Results Grid** - Virtualized data grid for large datasets
- **Context Menu** - Right-click tables for SELECT * or DESCRIBE TABLE (dialect-aware quoting)
- **Query History** - Dropdown with recent queries
- **SQL Cheatsheets** - Quick reference for SQLite, PostgreSQL, SQL Server and Oracle syntax
- **Export** - Save results as CSV or JSON
- **Resizable Panels** - Drag splitters to adjust proportions
- **Tabbed Queries** - Multiple query tabs with close buttons
- **Keyboard Shortcuts** - Ctrl+O, Ctrl+N, Ctrl+T, Ctrl+Enter, F5

## Download

Prebuilt, self-contained binaries (no .NET runtime required) are attached to every
[GitHub Release](https://github.com/tkleisas/SQLiteExplorer/releases):

| Platform | Asset |
|----------|-------|
| Windows x64 | `SQLiteExplorer-<version>-win-x64.zip` |
| Linux x64 | `SQLiteExplorer-<version>-linux-x64.tar.gz` |
| macOS (Apple Silicon) | `SQLiteExplorer-<version>-osx-arm64.tar.gz` |

Download, extract, and run - no installation required.

## Requirements

- .NET 10 SDK (to build from source)
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

4. Run the tests:
   ```bash
   dotnet test
   ```

   *If it doesn't work, please contact AI SLOPTRONIC (TM) support. We'll pretend to care.*

### Build Single File Executable

Build a self-contained single executable (no .NET runtime required on target machine):

```bash
dotnet publish src/SQLiteExplorer/SQLiteExplorer.csproj -c Release -r <rid>
```

Supported runtime identifiers: `win-x64`, `win-arm64`, `linux-x64`, `linux-arm64`,
`osx-x64`, `osx-arm64`. For example:

```bash
dotnet publish src/SQLiteExplorer/SQLiteExplorer.csproj -c Release -r win-x64
```

Output: `src/SQLiteExplorer/bin/Release/net10.0/<rid>/publish/SQLiteExplorer[.exe]`

> **Note:** Trimming is disabled because the SQL Server and Oracle ADO.NET drivers are
> reflection-heavy and not trim-safe, so the single-file binary is larger (~120 MB).

**Deployment:** Just copy the executable to the target machine - no installation required!

## Usage

### Connect to a Database

**SQLite:**
- **File → New → SQLite Database...** (Ctrl+N) - Create a new SQLite database
- **File → Open → SQLite Database...** (Ctrl+O) - Open an existing `.db`, `.sqlite`, or `.sqlite3` file

**PostgreSQL:**
- **File → Open → PostgreSQL Database...** - Enter host, port, database, username and password

**SQL Server:**
- **File → Open → SQL Server Database...** - Enter the server and database, then choose
  **Windows Authentication** or **SQL Server Authentication** (username/password). Connections
  trust the server certificate so local/dev instances work out of the box.

**Oracle:**
- **File → Open → Oracle Database...** - Two modes:
  - **EZConnect**: host, port (default 1521), service name, username and password
  - **Raw connection string / TNS**: paste a full ODP.NET connection string or one
    referencing a TNS alias as the Data Source
- Supports **Oracle Database 11g Release 2 and later** (via the ODP.NET Core 19c driver line).

### Run Queries
1. Type SQL in the editor
2. Click **Execute** or press **Ctrl+Enter** / **F5**
3. Results appear in tabs below
4. Multiple statements (separated by `;`) produce multiple result tabs

### Use Autocomplete
- Start typing to trigger autocomplete
- Press **Ctrl+Space** to force show completions
- Tables/columns from your database appear contextually
- *AI SLOPTRONIC (TM) reads your mind... or at least your schema*

### Right-Click Tables
- **SELECT \*** - Generate and execute a SELECT statement (schema-qualified, dialect-quoted)
- **DESCRIBE TABLE** - Show table structure

### View Cheatsheets
- **Help → SQLite / PostgreSQL / SQL Server / Oracle Cheatsheet**
- Cheatsheet panel appears on the right side
- Includes data types, queries, joins, functions, and more

### Export Results
- Click **CSV** or **JSON** buttons in each result tab
- Choose where to save the file

## Keyboard Shortcuts

| Shortcut | Action | AI SLOPTRONIC (TM) Rating |
|----------|--------|---------------------------|
| Ctrl+O | Open SQLite Database | ★★★★★ |
| Ctrl+N | New SQLite Database | ★★★★★ |
| Ctrl+T | New Query Tab | ★★★★☆ |
| Ctrl+Enter | Execute Query | ★★★★★ |
| Ctrl+Space | Show Autocomplete | ★★★★☆ |
| F5 | Execute Query (classic) | ★★★☆☆ |

## Menu Structure

```
File
├── New
│   ├── SQLite Database...
│   ├── PostgreSQL Database...
│   ├── SQL Server Database...
│   └── Oracle Database...
├── Open
│   ├── SQLite Database...
│   ├── PostgreSQL Database...
│   ├── SQL Server Database...
│   └── Oracle Database...
├── ─────────────
└── Exit

View
└── Refresh Tree

Query
└── New Query Tab

Help
├── SQLite Cheatsheet
├── PostgreSQL Cheatsheet
├── SQL Server Cheatsheet
├── Oracle Cheatsheet
├── ─────────────
└── About
```

## Architecture

The application is split into a thin executable and a reusable class library so the
database explorer can be embedded in other Avalonia apps.

```
SQLiteExplorer/
├── src/
│   ├── SQLiteExplorer/              # Desktop app shell (App, MainWindow, About)
│   └── SQLiteExplorer.Lib/          # Reusable library
│       ├── Models/                  # ConnectionInfo + per-dialect models, TableInfo, ...
│       ├── Services/                # IDatabaseService and providers:
│       │                            #   SqliteService, PostgresService,
│       │                            #   SqlServerService, OracleService,
│       │                            #   SqlStatementSplitter, DatabaseServiceFactory
│       ├── ViewModels/              # MVVM view models (MainWindowViewModel, ...)
│       ├── Views/                   # Avalonia views + connection dialogs
│       ├── Behaviors/               # XAML behaviors (autocomplete, text binding)
│       ├── Completion/              # SQL completion provider
│       └── Converters/              # Value converters
├── tests/
│   └── SQLiteExplorer.Lib.Tests/    # xUnit unit tests
├── lib/
│   └── AvaloniaVirtualDataGrid/     # Git submodule (virtualized data grid)
└── .github/workflows/               # ci.yml (build + test), release.yml (tag → release)
```

### Adding a database provider

Each provider implements `IDatabaseService`, which owns its dialect concerns
(`UsesSchemas`, `QuoteIdentifier`, `GetDescribeSql`) alongside connect/introspect/execute.
Register it in `DatabaseServiceFactory`, add a `ConnectionInfo` model and a connection
dialog, and wire up the menu commands.

## Testing

Unit tests live in `tests/SQLiteExplorer.Lib.Tests` (xUnit) and cover the pure logic:
SQL statement splitting, per-dialect identifier quoting and DESCRIBE SQL, the service
factory, and connection-string building.

```bash
dotnet test                  # run once
dotnet watch test --project tests/SQLiteExplorer.Lib.Tests   # TDD red-green loop
```

## Continuous Integration & Releases

- **CI** (`.github/workflows/ci.yml`) - builds the solution and runs the test suite on
  every push and pull request to `master`.
- **Release** (`.github/workflows/release.yml`) - triggered by pushing a `v*` tag. It
  builds self-contained single-file binaries for `win-x64`, `linux-x64` and `osx-arm64`
  and attaches them to a GitHub Release. The tag drives the version shown in the About
  dialog.

To cut a release from `master`:

```bash
git tag v1.1.0
git push origin v1.1.0
```

## Technologies

- [Avalonia UI](https://avaloniaui.net/) 12 - Cross-platform UI framework
- [AvaloniaEdit](https://github.com/AvaloniaUI/AvaloniaEdit) - Text editor with syntax highlighting
- [CommunityToolkit.Mvvm](https://learn.microsoft.com/dotnet/communitytoolkit/mvvm/) - MVVM helpers
- [Microsoft.Data.Sqlite](https://learn.microsoft.com/dotnet/standard/data/sqlite/) - SQLite ADO.NET provider
- [Npgsql](https://www.npgsql.org/) - PostgreSQL ADO.NET provider
- [Microsoft.Data.SqlClient](https://github.com/dotnet/SqlClient) - SQL Server ADO.NET provider
- [Oracle.ManagedDataAccess.Core](https://www.nuget.org/packages/Oracle.ManagedDataAccess.Core) - Oracle ADO.NET provider (ODP.NET Core)
- [AvaloniaVirtualDataGrid](https://github.com/tkleisas/AvaloniaVirtualDataGrid) - Virtualized data grid
- [GitInfo](https://github.com/devlooped/GitInfo) - Embeds git commit hash in builds
- [xUnit](https://xunit.net/) - Unit testing framework
- AI SLOPTRONIC (TM) - The finest in artificial slop generation technology

## Database Support

| Feature | SQLite | PostgreSQL | SQL Server | Oracle |
|---------|--------|------------|------------|--------|
| Connect | ✅ | ✅ | ✅ | ✅ |
| Create new database | ✅ | ➖ | ➖ | ➖ |
| Schema browsing | ✅ | ✅ | ✅ | ✅ |
| Multi-schema tree | flat | ✅ | ✅ | ✅ |
| Query execution | ✅ | ✅ | ✅ | ✅ |
| Multiple results | ✅ | ✅ | ✅ | ✅ |
| Table context menu | ✅ | ✅ | ✅ | ✅ |
| DESCRIBE TABLE | `PRAGMA table_info()` | `information_schema` | `INFORMATION_SCHEMA` | `ALL_TAB_COLUMNS` |
| Identifier quoting | `"x"` | `"x"` | `[x]` | `"X"` |
| Authentication | file | user/password | Windows or SQL | user/password, EZConnect/TNS |
| Minimum server version | - | - | - | 11g Release 2 |

## License

MIT License

*AI SLOPTRONIC (TM) is a trademark of absolutely nothing. Please don't sue us.*

---

*Made with 💜 and questionable amounts of AI assistance*
