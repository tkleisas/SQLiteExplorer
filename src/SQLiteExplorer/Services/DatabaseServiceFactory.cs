using System;
using SQLiteExplorer.Models;
using SQLiteExplorer.Services;

namespace SQLiteExplorer.Services;

public static class DatabaseServiceFactory
{
    public static IDatabaseService Create(DatabaseType databaseType)
    {
        return databaseType switch
        {
            DatabaseType.SQLite => new SqliteService(),
            DatabaseType.PostgreSQL => new PostgresService(),
            _ => throw new ArgumentException($"Unsupported database type: {databaseType}")
        };
    }
}
