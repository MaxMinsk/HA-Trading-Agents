# Binance MAF Trader

A **data-first, low-frequency** crypto trading research system on **C#/.NET 9 + Microsoft Agent Framework (MAF)**. MAF orchestrates a team of typed agents (analysts → bull/bear debate → trader → risk reviewer); the edge and correctness come from the data, validation, risk, and execution layers underneath — which we build first.

> **Status:** the data layer is in place — `Trading.Core` (domain contracts + the no-look-ahead snapshot invariant) and `Trading.Data` (SQLite store, Binance REST + live WebSocket ingestion, point-in-time snapshots, data-quality checks), driven by the `Trading.Cli` console host. 39 tests; `dotnet build -warnaserror` clean. The agent and risk layers are next. Backlog, sprints, and decisions live in the **Memory MCP** (`domain=development`, project `binance-maf-trader`), not in this repo.

## Why this shape

- **No look-ahead, ever** — a point-in-time `MarketSnapshot` may only contain data known at or before its decision time; enforced in code and tested.
- **Baselines before multi-agent** — a rules/single-agent baseline with fees & slippage must be beaten out-of-sample before the multi-agent workflow earns its cost.
- **Deterministic risk owns the trigger** — the LLM proposes; plain code validates, sizes, gates (stop / daily-loss / kill-switch), and can veto. No live order path without it.
- **Low-frequency** — 1h/4h/daily decisions; LLM cycles are too slow/expensive for scalping.

## Layout

```
src/Trading.Core    — storage/vendor-agnostic domain contracts (Candle, MarketSnapshot, TradeDecision, interfaces)
src/Trading.Data    — Binance ingestion (REST + live WebSocket), SQLite store, snapshots, data-quality  (TRD-001, done)
src/Trading.Cli     — console host: backfill history and stream live candles into the store             (TRD-001)
src/Trading.Agents  — MAF multi-agent workflow                                       (TRD-004, planned)
src/Trading.Risk    — deterministic risk + execution (testnet → live)               (TRD-003, planned)
tests/Trading.Tests — unit & integration tests
```

## Build & test

Requires the .NET 9 SDK (pinned via `global.json`).

```bash
dotnet restore
dotnet build -c Release          # CI adds -warnaserror to keep main zero-warning
dotnet test                      # all tests
```

## Run

```bash
# backfill historical candles into the SQLite store
dotnet run --project src/Trading.Cli -- backfill --symbols BTCUSDT,ETHUSDT --interval 1h --days 30

# stream live closed candles into the store (Ctrl+C to stop)
dotnet run --project src/Trading.Cli -- stream --symbols BTCUSDT,ETHUSDT --interval 1h
```

## Backlog (in Memory MCP)

```
# active sprint board
notes_search(domain="development", type="backlog_item",
             filter="payload.project == 'binance-maf-trader' AND payload.sprint == 'TRD-S1'",
             includePayload=true)
```

Current sprint **TRD-S1 — Data-first foundation**: `TRD-006` repo scaffolding (done), `TRD-001` data layer (done), `TRD-002` baseline + backtest harness (next).
