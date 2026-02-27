using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using SQLiteExplorer.Models;

namespace SQLiteExplorer.Services;

public class SqliteService : ISqliteService, IDisposable
{
    private SqliteConnection? _connection;
    
    public string? CurrentDatabasePath { get; private set; }
    public bool IsConnected => _connection != null && _connection.State == ConnectionState.Open;

    public async Task<bool> OpenDatabaseAsync(string path)
    {
        CloseDatabase();
        
        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = path,
            Mode = SqliteOpenMode.ReadWriteCreate
        };
        
        _connection = new SqliteConnection(builder.ConnectionString);
        await _connection.OpenAsync();
        CurrentDatabasePath = path;
        return true;
    }

    public void CloseDatabase()
    {
        if (_connection != null)
        {
            _connection.Close();
            _connection.Dispose();
            _connection = null;
            CurrentDatabasePath = null;
        }
    }

    public async Task<DatabaseInfo> GetDatabaseInfoAsync()
    {
        if (!IsConnected)
            throw new InvalidOperationException("No database is open");

        var info = new DatabaseInfo
        {
            Path = CurrentDatabasePath!,
            Name = Path.GetFileName(CurrentDatabasePath)
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

    public async Task<QueryResult> ExecuteQueryAsync(string sql)
    {
        if (!IsConnected)
            throw new InvalidOperationException("No database is open");

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
                var row = new System.Collections.Generic.Dictionary<string, object?>();
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
        CloseDatabase();
        GC.SuppressFinalize(this);
    }
}
