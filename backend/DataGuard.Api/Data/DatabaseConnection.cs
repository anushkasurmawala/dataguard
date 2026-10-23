using Npgsql;
namespace DataGuard.Api.Data;

public static class DatabaseConnection
{
    // Render supplies a postgres:// URL; Npgsql expects a connection string.
    public static string Resolve(string? databaseUrl, string? configured)
    {
        if (string.IsNullOrWhiteSpace(databaseUrl))
            return configured ?? throw new InvalidOperationException("A database connection is required.");
        var uri = new Uri(databaseUrl);
        if (uri.Scheme != "postgres" && uri.Scheme != "postgresql")
            throw new InvalidOperationException("DATABASE_URL must be a PostgreSQL URL.");
        var credentials = uri.UserInfo.Split(':', 2);
        var connection = new NpgsqlConnectionStringBuilder
        {
            Host = uri.Host,
            Port = uri.Port > 0 ? uri.Port : 5432,
            Database = Uri.UnescapeDataString(uri.AbsolutePath.TrimStart('/')),
            Username = Uri.UnescapeDataString(credentials[0]),
            Password = credentials.Length > 1 ? Uri.UnescapeDataString(credentials[1]) : "",
            SslMode = SslMode.Require
        };
        return connection.ConnectionString;
    }
}
