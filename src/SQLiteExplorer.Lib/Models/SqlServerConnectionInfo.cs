using System.Data.Common;
using Microsoft.Data.SqlClient;

namespace SQLiteExplorer.Lib.Models;

public class SqlServerConnectionInfo : ConnectionInfo
{
    public string Server { get; set; } = "localhost";
    public string Database { get; set; } = string.Empty;
    public bool UseWindowsAuth { get; set; }
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    /// <summary>
    /// Encryption behaviour: Mandatory (TLS required, the Microsoft.Data.SqlClient
    /// default), Optional (plaintext) or Strict (TDS 8.0, which always validates the
    /// certificate).
    /// </summary>
    public SqlConnectionEncryptOption Encrypt { get; set; } = SqlConnectionEncryptOption.Mandatory;

    /// <summary>
    /// When encrypting, accept the server certificate without validating it against
    /// the machine trust store. Required for local/dev instances that use a
    /// self-signed certificate. Ignored when Encrypt is Strict.
    /// </summary>
    public bool TrustServerCertificate { get; set; } = true;

    /// <summary>Seconds to wait while establishing the connection. 0 waits indefinitely.</summary>
    public int ConnectionTimeout { get; set; } = 15;

    /// <summary>
    /// Optional application name reported to SQL Server (visible in sp_who2, profiler,
    /// etc.). Empty omits the keyword and keeps the driver default.
    /// </summary>
    public string ApplicationName { get; set; } = string.Empty;

    /// <summary>Enables Multiple Active Result Sets (MARS) on the connection.</summary>
    public bool MultipleActiveResultSets { get; set; }

    /// <summary>
    /// Raw "Key=Value;..." pairs merged over the generated connection string; a key
    /// here overrides the built-in value (e.g. "Packet Size=8192;Connect Timeout=3").
    /// </summary>
    public string? AdditionalOptions { get; set; }

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
                Encrypt = Encrypt,
                ConnectTimeout = ConnectionTimeout,
                MultipleActiveResultSets = MultipleActiveResultSets
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

            // Strict (TDS 8.0) always validates the server certificate and does not
            // honour Trust Server Certificate, so only emit it for the other modes.
            if (Encrypt != SqlConnectionEncryptOption.Strict)
            {
                builder.TrustServerCertificate = TrustServerCertificate;
            }

            if (!string.IsNullOrWhiteSpace(ApplicationName))
            {
                builder.ApplicationName = ApplicationName;
            }

            if (!string.IsNullOrWhiteSpace(AdditionalOptions))
            {
                // Parse with the base builder, which only exposes the keys actually
                // present: a full SqlConnectionStringBuilder would materialise every
                // keyword with its default and wipe the values built above. Assigning
                // through the indexer makes a key here override the built-in value and
                // makes an invalid keyword fail fast here instead of during OpenAsync.
                var extra = new DbConnectionStringBuilder
                {
                    ConnectionString = AdditionalOptions.Trim().TrimEnd(';')
                };
                foreach (var key in extra.Keys)
                {
                    var keyword = key.ToString()!;
                    builder[keyword] = extra[keyword];
                }
            }

            return builder.ConnectionString;
        }
    }
}
