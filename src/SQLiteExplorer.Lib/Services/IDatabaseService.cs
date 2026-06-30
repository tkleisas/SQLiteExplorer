using System;
using System.Threading.Tasks;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

public interface IDatabaseService : IDisposable
{
    ConnectionInfo? ConnectionInfo { get; }
    bool IsConnected { get; }
    DatabaseType DatabaseType { get; }

    /// <summary>
    /// True when the database organizes objects into named schemas/owners and the
    /// object tree should show a schema level. SQLite returns false (flat tree).
    /// </summary>
    bool UsesSchemas { get; }

    Task<bool> ConnectAsync(ConnectionInfo connectionInfo);
    void Disconnect();
    Task<DatabaseInfo> GetDatabaseInfoAsync();
    Task<MultiQueryResult> ExecuteMultipleAsync(string sql);

    /// <summary>
    /// Returns a dialect-correct, optionally schema-qualified identifier reference
    /// (e.g. "schema"."table", [schema].[table], or "table").
    /// </summary>
    string QuoteIdentifier(string? schema, string name);

    /// <summary>
    /// Returns a dialect-specific statement that describes the columns of a table.
    /// </summary>
    string GetDescribeSql(string? schema, string name);
}
