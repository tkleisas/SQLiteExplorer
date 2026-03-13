namespace SQLiteExplorer.Lib.Models;

public class PostgresConnectionInfo : ConnectionInfo
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 5432;
    public string Database { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;

    public override string DisplayName => Database;
    public override DatabaseType DatabaseType => DatabaseType.PostgreSQL;

    public string ConnectionString => 
        $"Host={Host};Port={Port};Database={Database};Username={Username};Password={Password}";
}
