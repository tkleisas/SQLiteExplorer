namespace SQLiteExplorer.Models;

public class ColumnInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool NotNull { get; set; }
    public bool IsPrimaryKey { get; set; }
}
