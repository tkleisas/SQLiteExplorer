using System.Collections.Generic;

namespace SQLiteExplorer.Lib.Models;

public class TableInfo
{
    public string Name { get; set; } = string.Empty;
    public string Schema { get; set; } = string.Empty;
    public string Type { get; set; } = "table";
    public List<ColumnInfo> Columns { get; set; } = new();
}
