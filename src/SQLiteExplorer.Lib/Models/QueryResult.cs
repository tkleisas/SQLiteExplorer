using System.Collections.Generic;
using System.Linq;

namespace SQLiteExplorer.Lib.Models;

public class QueryResult
{
    public List<string> ColumnNames { get; set; } = new();
    public List<Dictionary<string, object?>> Rows { get; set; } = new();
    public int RowCount => Rows.Count;
    public long ExecutionTimeMs { get; set; }
    public string? ErrorMessage { get; set; }
    public bool IsSuccess => ErrorMessage == null;
    public int StatementIndex { get; set; }
}

public class MultiQueryResult
{
    public List<QueryResult> Results { get; set; } = new();
    public long TotalExecutionTimeMs { get; set; }
    public int TotalRows => Results.Sum(r => r.RowCount);
    public int SuccessCount => Results.Count(r => r.IsSuccess);
    public int ErrorCount => Results.Count(r => !r.IsSuccess);
    public bool HasErrors => Results.Any(r => !r.IsSuccess);
    public string? FirstError => Results.FirstOrDefault(r => !r.IsSuccess)?.ErrorMessage;
}
