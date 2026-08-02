using System;

namespace SQLiteExplorer.Lib.Models;

/// <summary>
/// A saved report definition: a named, reusable SQL query that can be
/// regenerated into an Excel report on demand.
/// </summary>
public class ReportDefinition
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Sql { get; set; } = string.Empty;
    public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;
    public DateTime ModifiedUtc { get; set; } = DateTime.UtcNow;
}
