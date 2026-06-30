namespace SQLiteExplorer.Lib.Models;

public class OracleConnectionInfo : ConnectionInfo
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 1521;
    public string ServiceName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// When set, this full ODP.NET connection string (or a string referencing a TNS
    /// alias as the Data Source) is used verbatim instead of building an EZConnect
    /// descriptor from Host/Port/ServiceName.
    /// </summary>
    public string? RawConnectionString { get; set; }

    public override string DisplayName =>
        !string.IsNullOrEmpty(ServiceName) ? ServiceName :
        !string.IsNullOrEmpty(Host) ? Host : "Oracle";

    public override DatabaseType DatabaseType => DatabaseType.Oracle;

    public string ConnectionString
    {
        get
        {
            if (!string.IsNullOrWhiteSpace(RawConnectionString))
                return RawConnectionString!;

            // EZConnect descriptor: host:port/service
            var dataSource = $"{Host}:{Port}/{ServiceName}";
            return $"User Id={Username};Password={Password};Data Source={dataSource}";
        }
    }
}
