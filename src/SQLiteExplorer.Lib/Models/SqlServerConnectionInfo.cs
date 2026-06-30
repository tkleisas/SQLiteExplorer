using Microsoft.Data.SqlClient;

namespace SQLiteExplorer.Lib.Models;

public class SqlServerConnectionInfo : ConnectionInfo
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = string.Empty;
    public bool UseWindowsAuth { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public override string DisplayName => string.IsNullOrEmpty(Database) ? Server : Database;
    public override DatabaseType DatabaseType => DatabaseType.SqlServer;

    public string ConnectionString
    {
        get
        {
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = Server,
                InitialCatalog = Database,
                // Microsoft.Data.SqlClient defaults Encrypt=True; trust the server
                // certificate so local/dev instances connect without a CA-signed cert.
                TrustServerCertificate = true
            };

            if (UseWindowsAuth)
            {
                builder.IntegratedSecurity = true;
            }
            else
            {
                builder.UserID = Username;
                builder.Password = Password;
            }

            return builder.ConnectionString;
        }
    }
}
