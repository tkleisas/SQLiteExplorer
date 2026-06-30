using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using Oracle.ManagedDataAccess.Client;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

public class OracleService : IDatabaseService
{
    private OracleConnection? _connection;
    private OracleConnectionInfo? _connectionInfo;

    public ConnectionInfo? ConnectionInfo => _connectionInfo;
    public bool IsConnected => _connection != null && _connection.State == ConnectionState.Open;
    public DatabaseType DatabaseType => DatabaseType.Oracle;
    public bool UsesSchemas => true;

    // Oracle-maintained / internal accounts to hide from the schema tree. Filtered via
    // an explicit list (rather than ALL_USERS.ORACLE_MAINTAINED, which only exists on
    // 12.1+) so introspection works on Oracle Database 11g Release 2 and later.
    private static readonly string[] SystemSchemas =
    {
        "SYS", "SYSTEM", "OUTLN", "DBSNMP", "APPQOSSYS", "WMSYS", "XDB", "ANONYMOUS",
        "CTXSYS", "MDSYS", "ORDSYS", "ORDDATA", "ORDPLUGINS", "SI_INFORMTN_SCHEMA",
        "OLAPSYS", "MDDATA", "SPATIAL_WFS_ADMIN_USR", "SPATIAL_CSW_ADMIN_USR",
        "EXFSYS", "LBACSYS", "DVSYS", "DVF", "AUDSYS", "GSMADMIN_INTERNAL",
        "GSMCATUSER", "GSMUSER", "SYSBACKUP", "SYSDG", "SYSKM", "SYSRAC", "DIP",
        "ORACLE_OCM", "REMOTE_SCHEDULER_AGENT", "XS$NULL", "OJVMSYS", "GGSYS",
        "SYS$UMF", "DBSFWUSER", "OWBSYS", "OWBSYS_AUDIT"
    };

    private static readonly string SystemSchemaList =
        string.Join(", ", Array.ConvertAll(SystemSchemas, s => $"'{s}'"));

    public string QuoteIdentifier(string? schema, string name) =>
        string.IsNullOrEmpty(schema)
            ? $"\"{name.Replace("\"", "\"\"")}\""
            : $"\"{schema.Replace("\"", "\"\"")}\".\"{name.Replace("\"", "\"\"")}\"";

    // No trailing semicolon: a plain (non-PL/SQL) statement sent to OracleCommand
    // must not be terminated with ';' (ORA-00911).
    public string GetDescribeSql(string? schema, string name) =>
        "SELECT COLUMN_NAME, DATA_TYPE, NULLABLE FROM ALL_TAB_COLUMNS " +
        $"WHERE OWNER = '{schema}' AND TABLE_NAME = '{name}' ORDER BY COLUMN_ID";

    public async Task<bool> ConnectAsync(ConnectionInfo connectionInfo)
    {
        if (connectionInfo is not OracleConnectionInfo oracleInfo)
            throw new ArgumentException("Invalid connection info type", nameof(connectionInfo));

        Disconnect();

        _connection = new OracleConnection(oracleInfo.ConnectionString);
        await _connection.OpenAsync();
        _connectionInfo = oracleInfo;
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
            Name = _connectionInfo.DisplayName
        };

        var command = _connection!.CreateCommand();
        command.BindByName = true;
        // Show user schemas only, excluding Oracle-maintained accounts. Uses a static
        // exclusion list plus APEX/FLOWS prefixes so it runs on 11g (which lacks
        // ALL_USERS.ORACLE_MAINTAINED).
        command.CommandText = $@"
            SELECT OWNER, TABLE_NAME, 'table' AS OBJ_TYPE
            FROM ALL_TABLES
            WHERE OWNER NOT IN ({SystemSchemaList})
              AND OWNER NOT LIKE 'APEX\_%' ESCAPE '\'
              AND OWNER NOT LIKE 'FLOWS\_%' ESCAPE '\'
            UNION ALL
            SELECT OWNER, VIEW_NAME, 'view' AS OBJ_TYPE
            FROM ALL_VIEWS
            WHERE OWNER NOT IN ({SystemSchemaList})
              AND OWNER NOT LIKE 'APEX\_%' ESCAPE '\'
              AND OWNER NOT LIKE 'FLOWS\_%' ESCAPE '\'
            ORDER BY 1, 2";

        using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                var table = new TableInfo
                {
                    Schema = reader.GetString(0),
                    Name = reader.GetString(1),
                    Type = reader.GetString(2)
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
        command.BindByName = true;
        command.CommandText = @"
            SELECT COLUMN_NAME, DATA_TYPE, NULLABLE
            FROM ALL_TAB_COLUMNS
            WHERE OWNER = :owner AND TABLE_NAME = :tableName
            ORDER BY COLUMN_ID";

        command.Parameters.Add("owner", table.Schema);
        command.Parameters.Add("tableName", table.Name);

        using (command)
        {
            using var reader = await command.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                table.Columns.Add(new ColumnInfo
                {
                    Name = reader.GetString(0),
                    Type = reader.GetString(1),
                    NotNull = reader.GetString(2) == "N",
                    IsPrimaryKey = false
                });
            }
        }

        await LoadPrimaryKeysAsync(table);
    }

    private async Task LoadPrimaryKeysAsync(TableInfo table)
    {
        var command = _connection!.CreateCommand();
        command.BindByName = true;
        command.CommandText = @"
            SELECT cc.COLUMN_NAME
            FROM ALL_CONSTRAINTS c
            JOIN ALL_CONS_COLUMNS cc
                ON c.CONSTRAINT_NAME = cc.CONSTRAINT_NAME
                AND c.OWNER = cc.OWNER
            WHERE c.CONSTRAINT_TYPE = 'P'
                AND c.OWNER = :owner
                AND c.TABLE_NAME = :tableName";

        command.Parameters.Add("owner", table.Schema);
        command.Parameters.Add("tableName", table.Name);

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

        var statements = SplitStatements(sql);

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

    private static List<string> SplitStatements(string sql)
    {
        var statements = new List<string>();
        var current = new StringBuilder();
        var inString = false;
        var stringChar = '\0';

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];

            if (!inString && c == '\'')
            {
                inString = true;
                stringChar = c;
                current.Append(c);
            }
            else if (inString && c == stringChar)
            {
                if (i + 1 < sql.Length && sql[i + 1] == stringChar)
                {
                    current.Append(c);
                    current.Append(c);
                    i++;
                }
                else
                {
                    inString = false;
                    current.Append(c);
                }
            }
            else if (!inString && c == ';')
            {
                var stmt = current.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(stmt))
                {
                    statements.Add(stmt);
                }
                current.Clear();
            }
            else if (!inString && c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                while (i < sql.Length && sql[i] != '\n') i++;
            }
            else if (!inString && c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i < sql.Length - 1 && !(sql[i] == '*' && sql[i + 1] == '/')) i++;
                i++;
            }
            else
            {
                current.Append(c);
            }
        }

        var lastStmt = current.ToString().Trim();
        if (!string.IsNullOrWhiteSpace(lastStmt))
        {
            statements.Add(lastStmt);
        }

        return statements;
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
