using Microsoft.Data.Sqlite;

namespace Trading.Data.Storage;

/// <summary>
/// Creates open SQLite connections to the market-data database, with WAL journaling and a
/// busy-timeout configured in code (per the HAMemory convention — concurrency via WAL, not async).
/// </summary>
public sealed class SqliteConnectionFactory
{
    private readonly string _connectionString;

    /// <summary>Creates a factory for the database at <paramref name="databasePath"/> (created on first open).</summary>
    /// <param name="databasePath">Path to the SQLite database file.</param>
    public SqliteConnectionFactory(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);
        _connectionString = new SqliteConnectionStringBuilder
        {
            DataSource = databasePath,
            Mode = SqliteOpenMode.ReadWriteCreate,
        }.ToString();
    }

    /// <summary>Opens a new connection with WAL and a 5s busy-timeout applied.</summary>
    /// <returns>An open <see cref="SqliteConnection"/> the caller owns and must dispose.</returns>
    public SqliteConnection Open()
    {
        var connection = new SqliteConnection(_connectionString);
        connection.Open();
        using var pragma = connection.CreateCommand();
        pragma.CommandText = "PRAGMA journal_mode=WAL; PRAGMA busy_timeout=5000;";
        pragma.ExecuteNonQuery();
        return connection;
    }
}
