namespace SQLiteExplorer.Lib.Models;

public abstract class ConnectionInfo
{
    public abstract string DisplayName { get; }
    public abstract DatabaseType DatabaseType { get; }
}

public enum DatabaseType
{
    SQLite,
    PostgreSQL,
    SqlServer,
    Oracle
}
