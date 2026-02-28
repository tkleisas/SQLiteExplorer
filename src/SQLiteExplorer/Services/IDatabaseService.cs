using System;
using System.Threading.Tasks;
using SQLiteExplorer.Models;

namespace SQLiteExplorer.Services;

public interface IDatabaseService : IDisposable
{
    ConnectionInfo? ConnectionInfo { get; }
    bool IsConnected { get; }
    DatabaseType DatabaseType { get; }
    
    Task<bool> ConnectAsync(ConnectionInfo connectionInfo);
    void Disconnect();
    Task<DatabaseInfo> GetDatabaseInfoAsync();
    Task<MultiQueryResult> ExecuteMultipleAsync(string sql);
}
