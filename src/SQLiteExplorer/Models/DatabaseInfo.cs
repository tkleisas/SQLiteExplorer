using System.Collections.Generic;

namespace SQLiteExplorer.Models;

public class DatabaseInfo
{
    public string Path { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public List<TableInfo> Tables { get; set; } = new();
}
