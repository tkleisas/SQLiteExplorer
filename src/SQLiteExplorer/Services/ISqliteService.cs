using System.Threading.Tasks;
using SQLiteExplorer.Models;

namespace SQLiteExplorer.Services;

public interface ISqliteService
{
    string? CurrentDatabasePath { get; }
    bool IsConnected { get; }
    
    Task<bool> OpenDatabaseAsync(string path);
    void CloseDatabase();
    Task<DatabaseInfo> GetDatabaseInfoAsync();
    Task<QueryResult> ExecuteQueryAsync(string sql);
    Task<MultiQueryResult> ExecuteMultipleAsync(string sql);
}
