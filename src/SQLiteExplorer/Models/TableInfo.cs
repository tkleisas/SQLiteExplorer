using System.Collections.Generic;

namespace SQLiteExplorer.Models;

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = "table";
    public List<ColumnInfo> Columns { get; set; } = new();
}
