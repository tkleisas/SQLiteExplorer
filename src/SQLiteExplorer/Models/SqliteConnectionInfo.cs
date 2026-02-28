namespace SQLiteExplorer.Models;

public class SqliteConnectionInfo : ConnectionInfo
{
    public string FilePath { get; set; } = string.Empty;

    public override string DisplayName => System.IO.Path.GetFileName(FilePath);
    public override DatabaseType DatabaseType => DatabaseType.SQLite;
}
