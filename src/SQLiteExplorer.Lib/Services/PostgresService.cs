using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Npgsql;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

public class PostgresService : IDatabaseService
{
    private NpgsqlConnection? _connection;
    private PostgresConnectionInfo? _connectionInfo;

    public ConnectionInfo? ConnectionInfo => _connectionInfo;
    public bool IsConnected => _connection != null && _connection.State == ConnectionState.Open;
    public DatabaseType DatabaseType => DatabaseType.PostgreSQL;
    public bool UsesSchemas => true;

    public string QuoteIdentifier(string? schema, string name) =>
        string.IsNullOrEmpty(schema)
            ? $"\"{name.Replace("\"", "\"\"")}\""
            : $"\"{schema.Replace("\"", "\"\"")}\".\"{name.Replace("\"", "\"\"")}\"";

    public string GetDescribeSql(string? schema, string name) =>
        "SELECT column_name, data_type, is_nullable FROM information_schema.columns " +
        $"WHERE table_schema = '{schema}' AND table_name = '{name}' ORDER BY ordinal_position;";

    public async Task<bool> ConnectAsync(ConnectionInfo connectionInfo)
    {
        if (connectionInfo is not PostgresConnectionInfo postgresInfo)
            throw new ArgumentException("Invalid connection info type", nameof(connectionInfo));

        Disconnect();

        _connection = new NpgsqlConnection(postgresInfo.ConnectionString);
        await _connection.OpenAsync();
        _connectionInfo = postgresInfo;
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
            Path = _connectionInfo.Host,
            Name = _connectionInfo.Database
        };

        var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT table_schema, table_name, table_type
            FROM information_schema.tables
            WHERE table_schema NOT IN ('pg_catalog', 'information_schema')
              AND table_type IN ('BASE TABLE', 'VIEW')
            ORDER BY table_schema, table_name";

        using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var schemaName = reader.GetString(0);
                var tableName = reader.GetString(1);
                var tableType = reader.GetString(2) == "VIEW" ? "view" : "table";

                var table = new TableInfo
                {
                    Schema = schemaName,
                    Name = tableName,
                    Type = tableType
                };

                info.Tables.Add(table);
            }
        }

        foreach (var table in info.Tables)
        {
            await LoadColumnsAsync(table);
        }

        return info;
    }

    private async Task LoadColumnsAsync(TableInfo table)
    {
        var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT column_name, data_type, is_nullable
            FROM information_schema.columns
            WHERE table_schema = @schemaName AND table_name = @tableName
            ORDER BY ordinal_position";

        command.Parameters.AddWithValue("schemaName", table.Schema);
        command.Parameters.AddWithValue("tableName", table.Name);

        using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                table.Columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    Type = reader.GetString(1),
                    NotNull = reader.GetString(2) == "NO",
                    IsPrimaryKey = false
                });
            }
        }

        await LoadPrimaryKeysAsync(table);
    }

    private async Task LoadPrimaryKeysAsync(TableInfo table)
    {
        var command = _connection!.CreateCommand();
        command.CommandText = @"
            SELECT kcu.column_name
            FROM information_schema.table_constraints tc
            JOIN information_schema.key_column_usage kcu 
                ON tc.constraint_name = kcu.constraint_name
            WHERE tc.table_schema = @schemaName
                AND tc.table_name = @tableName
                AND tc.constraint_type = 'PRIMARY KEY'";

        command.Parameters.AddWithValue("schemaName", table.Schema);
        command.Parameters.AddWithValue("tableName", table.Name);

        using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var columnName = reader.GetString(0);
                var column = table.Columns.Find(c => c.Name == columnName);
                if (column != null)
                {
                    column.IsPrimaryKey = true;
                }
            }
        }
    }

    public async Task<MultiQueryResult> ExecuteMultipleAsync(string sql)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No database is open");

        var totalSw = System.Diagnostics.Stopwatch.StartNew();
        var multiResult = new MultiQueryResult();

        var statements = SqlStatementSplitter.Split(sql);

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
