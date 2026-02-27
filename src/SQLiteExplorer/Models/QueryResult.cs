using System.Collections.Generic;

namespace SQLiteExplorer.Models;

public class QueryResult
{
    public List<string> ColumnNames { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount => Rows.Count;
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => ErrorMessage == null;
}
