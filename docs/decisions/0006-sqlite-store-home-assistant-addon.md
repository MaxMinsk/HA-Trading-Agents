# 0006 — SQLite market-data store, deployed as a Home Assistant add-on

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-storage-sqlite-ha-addon`

## Decision

Use **SQLite** as the market-data store for the MVP, and deploy the trader the same way as the Memory MCP — as a **Home Assistant add-on** (Docker image on GHCR, `/data` volume). The store uses WAL + `busy_timeout` set in code and a `PRAGMA user_version` migration ladder; decimals are stored as invariant text and times as UTC Unix milliseconds.

## Context

The user prefers to reuse the proven HAMemory deployment/runtime stack (HA add-on, `/data` volume, GHCR, `Microsoft.Data.Sqlite`). The MVP is low-frequency (1h/4h/1d candles for BTC/ETH), which is well within SQLite's range. Storage is hidden behind `IMarketDataStore`, so a columnar/time-series engine (DuckDB+Parquet, TimescaleDB) can replace it later without touching callers.

## Consequence

- `Trading.Data` ships `SqliteConnectionFactory`, `MarketDataMigrator`, and `SqliteMarketDataStore`.
- Packaging as an HA add-on is tracked by **TRD-007**.
- `Microsoft.Data.Sqlite` pulls a transitive `SQLitePCLRaw` flagged by NU1903 (GHSA-2m69-gcr7-jv3q) with no patched upstream release yet; it is suppressed with a rationale in `Directory.Build.props` and tracked by **TRD-008**.
- Revisit the storage engine only if low-frequency SQLite becomes a bottleneck.
