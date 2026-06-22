# Binance MAF Trader

A **data-first, low-frequency** crypto trading research system on **C#/.NET 9 + Microsoft Agent Framework (MAF)**. MAF orchestrates a team of typed agents (analysts → bull/bear debate → trader → risk reviewer); the edge and correctness come from the data, validation, risk, and execution layers underneath — which we build first.

> **Status:** the data layer (with a backtest harness) is exposed over **MCP** and an agent host consumes it. `Trading.Core` (contracts + the no-look-ahead snapshot invariant), `Trading.Data` (SQLite store, Binance REST + live WebSocket ingestion, snapshots, data-quality), `Trading.Backtest` (strategies + metrics + buy&hold), `Trading.Mcp` (the market-data MCP server / HA add-on with in-process ingestion), and `Trading.Agent` (a relocatable MCP client). 54 tests; `dotnet build -warnaserror` clean. The MAF multi-agent workflow and the risk/execution layer are next. Backlog, sprints, and decisions live in the **Memory MCP** (`domain=development`, project `binance-maf-trader`), not in this repo.

## Why this shape

- **No look-ahead, ever** — a point-in-time `MarketSnapshot` may only contain data known at or before its decision time; enforced in code and tested.
- **Baselines before multi-agent** — a rules/single-agent baseline with fees & slippage must be beaten out-of-sample before the multi-agent workflow earns its cost.
- **Deterministic risk owns the trigger** — the LLM proposes; plain code validates, sizes, gates (stop / daily-loss / kill-switch), and can veto. No live order path without it.
- **Low-frequency** — 1h/4h/daily decisions; LLM cycles are too slow/expensive for scalping.
- **Data over MCP** — the data layer runs as a Home Assistant add-on exposing market data via MCP; the agent is a relocatable MCP client (runs locally where Anthropic/OpenAI APIs are reachable, or inside the add-on). Order execution will be MCP write-tools on the server too (TRD-003).

## Layout

```
src/Trading.Core     — storage/vendor-agnostic contracts (Candle, MarketSnapshot, TradeDecision, IStrategy, interfaces)
src/Trading.Data     — Binance ingestion (REST + live WebSocket), SQLite store, snapshots, data-quality   (TRD-001)
src/Trading.Backtest — strategies, backtest engine, performance metrics, buy&hold benchmark              (TRD-S2)
src/Trading.Mcp      — market-data MCP server (HTTP+bearer / stdio) + in-process ingestion; the HA add-on (TRD-S3)
src/Trading.Agent    — relocatable agent host: an MCP client of the data layer (MAF workflow lands in TRD-004)
src/Trading.Cli      — console host: backfill, stream, backtest                                           (TRD-001 / TRD-S2)
src/Trading.Risk     — deterministic risk + execution, exposed over MCP (testnet → live)      (TRD-003, planned)
addon/               — Home Assistant add-on packaging for the data MCP (Dockerfile, config.yaml, run.sh)
tests/Trading.Tests  — unit & integration tests
```

## Requirements

- **.NET 9 SDK** (pinned via `global.json`).
- **A Memory MCP server** — the project keeps its backlog, sprints, decisions, and the agent layer's durable memory/audit in [Memory MCP](https://github.com/MaxMinsk/HomeMemory) (`domain=development`, project `binance-maf-trader`), not in this repo. Agents connect to it over MCP to recall context and record decisions.
- **Reachable model APIs for the agent layer** — the planned LLM/agent layer calls Anthropic/OpenAI, so it must run where those APIs are reachable (e.g. locally); it is not expected to run on a geo-restricted server. The data layer (backfill/stream) has no such requirement.

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

# backtest a strategy vs buy & hold over stored candles (net of fees/slippage)
dotnet run --project src/Trading.Cli -- backtest --symbols BTCUSDT --interval 1h --strategy sma --fast 20 --slow 50 --days 120
```

## Run the data MCP server + agent

```bash
# data MCP server (HTTP + bearer) — the HA add-on runs this; locally:
TRADING_TRANSPORT=http TRADING_BEARER_TOKEN=dev TRADING_INGEST=false \
  ASPNETCORE_URLS=http://127.0.0.1:8080 dotnet run --project src/Trading.Mcp

# agent host: an MCP client of the data layer (same code locally or in the add-on — config only)
TRADING_MCP_URL=http://127.0.0.1:8080/mcp TRADING_BEARER_TOKEN=dev \
  dotnet run --project src/Trading.Agent -- --symbol BTCUSDT --interval 1h
```

## Backlog (in Memory MCP)

```
# active sprint board
notes_search(domain="development", type="backlog_item",
             filter="payload.project == 'binance-maf-trader' AND payload.sprint == 'TRD-S3'",
             includePayload=true)
```

Current sprint **TRD-S3 — Market-data MCP add-on + agent client**: the data layer as an HA add-on exposing market data over MCP, in-add-on ingestion, an MCP-client agent host, and build CI. (TRD-S1 data-first and TRD-S2 baseline+backtest are done.)
