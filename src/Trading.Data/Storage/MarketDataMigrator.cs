using Microsoft.Data.Sqlite;

namespace Trading.Data.Storage;

/// <summary>
/// Applies the market-data schema migration ladder using <c>PRAGMA user_version</c>. Each step is
/// applied once and in order; shipped steps are never edited (add the next version instead).
/// </summary>
public static class MarketDataMigrator
{
    private const string Migration1Candles = """
        CREATE TABLE IF NOT EXISTS candles (
            symbol         TEXT    NOT NULL,
            market         INTEGER NOT NULL,
            interval       INTEGER NOT NULL,
            open_time_ms   INTEGER NOT NULL,
            close_time_ms  INTEGER NOT NULL,
            open           TEXT    NOT NULL,
            high           TEXT    NOT NULL,
            low            TEXT    NOT NULL,
            close          TEXT    NOT NULL,
            volume         TEXT    NOT NULL,
            source         TEXT    NOT NULL,
            ingested_at_ms INTEGER NOT NULL,
            PRIMARY KEY (symbol, market, interval, open_time_ms)
        );
        CREATE INDEX IF NOT EXISTS ix_candles_asof
            ON candles (symbol, market, interval, close_time_ms);
        """;

    /// <summary>Brings the database schema up to the latest version.</summary>
    /// <param name="connection">An open connection.</param>
    public static void Apply(SqliteConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        if (GetUserVersion(connection) < 1)
        {
            Execute(connection, Migration1Candles);
            Execute(connection, "PRAGMA user_version = 1;");
        }
    }

    private static long GetUserVersion(SqliteConnection connection)
    {
        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA user_version;";
        return (long)(command.ExecuteScalar() ?? 0L);
    }

    private static void Execute(SqliteConnection connection, string sql)
    {
        using var command = connection.CreateCommand();
        command.CommandText = sql;
        command.ExecuteNonQuery();
    }
}
