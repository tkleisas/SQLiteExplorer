using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

public class SqliteService : IDatabaseService
{
    private SqliteConnection? _connection;
    private SqliteConnectionInfo? _connectionInfo;

    public ConnectionInfo? ConnectionInfo => _connectionInfo;
    public bool IsConnected => _connection != null && _connection.State == ConnectionState.Open;
    public DatabaseType DatabaseType => DatabaseType.SQLite;
    public bool UsesSchemas => false;

    public string QuoteIdentifier(string? schema, string name) =>
        $"\"{name.Replace("\"", "\"\"")}\"";

    public string GetDescribeSql(string? schema, string name) =>
        $"PRAGMA table_info({QuoteIdentifier(schema, name)})";

    public async Task<bool> ConnectAsync(ConnectionInfo connectionInfo)
    {
        if (connectionInfo is not SqliteConnectionInfo sqliteInfo)
            throw new ArgumentException("Invalid connection info type", nameof(connectionInfo));

        Disconnect();

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = sqliteInfo.FilePath,
            Mode = SqliteOpenMode.ReadWriteCreate
        };

        _connection = new SqliteConnection(builder.ConnectionString);
        await _connection.OpenAsync();
        _connectionInfo = sqliteInfo;
        return true;
    }

    public void Disconnect()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
            _connectionInfo = null;
        }
    }

    public async Task<DatabaseInfo> GetDatabaseInfoAsync()
    {
        if (!IsConnected || _connectionInfo == null)
            throw new InvalidOperationException("No database is open");

        var info = new DatabaseInfo
        {
            Path = _connectionInfo.FilePath,
            Name = Path.GetFileName(_connectionInfo.FilePath)
        };

        var command = _connection!.CreateCommand();
        command.CommandText = "SELECT name, type FROM sqlite_master WHERE type IN ('table', 'view') AND name NOT LIKE 'sqlite_%' ORDER BY type, name";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            var tableName = reader.GetString(0);
            var tableType = reader.GetString(1);

            var table = new TableInfo
            {
                Name = tableName,
                Type = tableType
            };

            await LoadColumnsAsync(table);
            info.Tables.Add(table);
        }

        return info;
    }

    private async Task LoadColumnsAsync(TableInfo table)
    {
        var command = _connection!.CreateCommand();
        command.CommandText = $"PRAGMA table_info(\"{table.Name.Replace("\"", "\"\"")}\")";

        using var reader = await command.ExecuteReaderAsync();
        while (await reader.ReadAsync())
        {
            table.Columns.Add(new ColumnInfo
            {
                Name = reader.GetString(1),
                Type = reader.GetString(2),
                NotNull = reader.GetInt32(3) == 1,
                IsPrimaryKey = reader.GetInt32(5) == 1
            });
        }
    }

    public async Task<MultiQueryResult> ExecuteMultipleAsync(string sql)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No database is open");

        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var multiResult = new MultiQueryResult();

        var statements = SqlStatementSplitter.Split(sql, treatDoubleQuoteAsString: true);

        for (var i = 0; i < statements.Count; i++)
        {
            var statement = statements[i].Trim();
            if (string.IsNullOrWhiteSpace(statement)) continue;

            var result = await ExecuteStatementAsync(statement);
            result.StatementIndex = i;
            multiResult.Results.Add(result);
        }

        totalSw.Stop();
        multiResult.TotalExecutionTimeMs = totalSw.ElapsedMilliseconds;

        return multiResult;
    }

    private async Task<QueryResult> ExecuteStatementAsync(string sql)
    {
        var sw = System.Diagnostics.Stopwatch.StartNew();
        var result = new QueryResult();

        try
        {
            var command = _connection!.CreateCommand();
            command.CommandText = sql;

            using var reader = await command.ExecuteReaderAsync();

            for (var i = 0; i < reader.FieldCount; i++)
            {
                result.ColumnNames.Add(reader.GetName(i));
            }

            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object?>();
                for (var i = 0; i < reader.FieldCount; i++)
                {
                    var value = reader.GetValue(i);
                    row[result.ColumnNames[i]] = value == DBNull.Value ? null : value;
                }
                result.Rows.Add(row);
            }

            sw.Stop();
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        }
        catch (Exception ex)
        {
            sw.Stop();
            result.ErrorMessage = ex.Message;
            result.ExecutionTimeMs = sw.ElapsedMilliseconds;
        }

        return result;
    }

    public void Dispose()
    {
        Disconnect();
        GC.SuppressFinalize(this);
    }
}
