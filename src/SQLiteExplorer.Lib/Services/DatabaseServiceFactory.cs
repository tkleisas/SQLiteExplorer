using System;
using SQLiteExplorer.Lib.Models;

namespace SQLiteExplorer.Lib.Services;

public static class DatabaseServiceFactory
{
    public static IDatabaseService Create(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SQLite => new SqliteService(),
            DatabaseType.PostgreSQL => new PostgresService(),
            DatabaseType.SqlServer => new SqlServerService(),
            DatabaseType.Oracle => new OracleService(),
            _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
        };
    }
}
