# Binance MAF Trader

A **data-first, low-frequency** crypto trading research system on **C#/.NET 9 + Microsoft Agent Framework (MAF)**. MAF orchestrates a team of typed agents (analysts → bull/bear debate → trader → risk reviewer); the edge and correctness come from the data, validation, risk, and execution layers underneath — which we build first.

> **Status:** the data layer (with a backtest harness) is exposed over **MCP**, a **MAF multi-agent crew** consumes it, and a **deterministic risk + execution layer** is exposed as MCP write-tools (paper / Binance testnet). `Trading.Core` (contracts + the no-look-ahead snapshot invariant + execution contracts), `Trading.Data` (SQLite store, Binance REST + live WebSocket ingestion, snapshots, data-quality), `Trading.Backtest` (strategies + metrics + buy&hold), `Trading.Risk` (limits + the gate), `Trading.Execution` (execution service + paper & Binance-testnet adapters), `Trading.Agents` (the MAF crew — provider-abstracted over Anthropic + OpenAI), `Trading.Mcp` (the market-data + execution MCP server / HA add-on with in-process ingestion), `Trading.Agent` (a relocatable MCP-client host that runs the crew), `Trading.Api` (an ASP.NET backend), and `Trading.Web` (a React + TypeScript web UI). 110 .NET tests + 17 web tests; `dotnet build -warnaserror` and the web lint/build/test are clean. Backtesting the crew against the TRD-002 baselines is next. Backlog, sprints, and decisions live in the **Memory MCP** (`domain=development`, project `binance-maf-trader`), not in this repo.

## Why this shape

- **No look-ahead, ever** — a point-in-time `MarketSnapshot` may only contain data known at or before its decision time; enforced in code and tested.
- **Baselines before multi-agent** — a rules/single-agent baseline with fees & slippage must be beaten out-of-sample before the multi-agent workflow earns its cost. The crew is an `IStrategy`, so it backtests against the same baselines.
- **Provider-abstracted agents** — the crew runs on one `IChatClient` seam (Microsoft.Extensions.AI); Anthropic and OpenAI are interchangeable by config, and MAF/SDK calls are confined to small adapters. The trader's output is parsed fail-closed to Hold.
- **Deterministic risk owns the trigger** — the LLM proposes; plain code validates, sizes, gates (stop / daily-loss / kill-switch), and can veto. No live order path without it.
- **Low-frequency** — 1h/4h/daily decisions; LLM cycles are too slow/expensive for scalping.
- **Data over MCP** — the data layer runs as a Home Assistant add-on exposing market data via MCP; the agent is a relocatable MCP client (runs locally where Anthropic/OpenAI APIs are reachable, or inside the add-on). Order execution is MCP write-tools on the server too, behind the risk gate (paper by default, Binance testnet next, live opt-in) — keys never leave the server.

## Layout

```
src/Trading.Core     — storage/vendor-agnostic contracts (Candle, MarketSnapshot, TradeDecision, IStrategy, interfaces)
src/Trading.Data     — Binance ingestion (REST + live WebSocket), SQLite store, snapshots, data-quality   (TRD-001)
src/Trading.Backtest — strategies, backtest engine, performance metrics, buy&hold benchmark              (TRD-S2)
src/Trading.Risk     — deterministic risk limits + gate (size clamp, daily-loss stop, kill-switch)        (TRD-S4)
src/Trading.Execution— execution service + paper adapter + Binance testnet adapter (HMAC-signed REST)      (TRD-S4)
src/Trading.Agents   — MAF multi-agent crew (analyst/bull/bear/trader/risk), provider-abstracted          (TRD-S5)
src/Trading.Mcp      — market-data + execution MCP server (HTTP+bearer / stdio) + in-process ingestion     (TRD-S3 / TRD-S4)
src/Trading.Agent    — relocatable host: an MCP client that runs the crew (or an SMA fallback) on a snapshot (TRD-S5)
src/Trading.Cli      — console host: backfill, stream, backtest                                           (TRD-001 / TRD-S2)
src/Trading.Api      — ASP.NET backend: serves the web UI + crew SSE stream and MCP proxies              (TRD-S6)
src/Trading.Web      — React + TypeScript web UI (Vite / Tailwind / Zustand), mirrors PFlow              (TRD-S6)
addon/               — Home Assistant add-on packaging for the MCP server (Dockerfile, config.yaml, run.sh)
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

# agent host (SMA fallback — no LLM key needed; same code locally or in the add-on)
TRADING_MCP_URL=http://127.0.0.1:8080/mcp TRADING_BEARER_TOKEN=dev \
  dotnet run --project src/Trading.Agent -- --symbol BTCUSDT --interval 1h --strategy sma
```

## Run the MAF multi-agent crew

The crew (analyst → bull/bear debate → trader → risk reviewer) is provider-abstracted: set a provider,
model, and key. It runs **locally**, where the model APIs are reachable. With no key configured the host
falls back to the SMA strategy, so the loop still works on the geo-restricted server.

```bash
# Anthropic (Claude)
TRADING_MCP_URL=http://127.0.0.1:8080/mcp TRADING_BEARER_TOKEN=dev \
  TRADING_LLM_PROVIDER=anthropic TRADING_LLM_MODEL=claude-sonnet-4-6 ANTHROPIC_API_KEY=sk-ant-... \
  dotnet run --project src/Trading.Agent -- --symbol BTCUSDT --interval 1h

# OpenAI (set TRADING_LLM_PROVIDER=openai, TRADING_LLM_MODEL, OPENAI_API_KEY)
# Optional: TRADING_AGENT_SUBMIT=true forwards a non-hold decision to the execution MCP (exec_submit_intent).
```

## Enable execution (paper / Binance testnet)

Execution MCP write-tools (`exec_submit_intent`, `exec_cancel`, `account_balances`) are **off by default**;
enable them explicitly. Every order runs through the deterministic risk gate server-side. The default
adapter is the **paper** simulator (no keys); the **Binance** adapter targets the **testnet** unless
`TRADING_BINANCE_LIVE=true`. Keys are read on the server and never leave it.

```bash
# paper execution (no keys) — risk gate + simulated fills
TRADING_TRANSPORT=http TRADING_BEARER_TOKEN=dev TRADING_INGEST=true \
  TRADING_EXEC_ENABLED=true TRADING_EXEC_MODE=paper TRADING_PAPER_QUOTE=10000 \
  ASPNETCORE_URLS=http://127.0.0.1:8080 dotnet run --project src/Trading.Mcp

# Binance spot testnet — keys from https://testnet.binance.vision (server-only)
TRADING_EXEC_ENABLED=true TRADING_EXEC_MODE=binance \
  TRADING_BINANCE_API_KEY=... TRADING_BINANCE_API_SECRET=... TRADING_BINANCE_LIVE=false \
  ...
```

Risk limits are configurable: `TRADING_MAX_POSITION_FRACTION` (0.25), `TRADING_MAX_ORDER_NOTIONAL` (1000),
`TRADING_DAILY_LOSS_FRACTION` (0.05), `TRADING_ALLOW_SHORTING` (false), `TRADING_KILL_SWITCH` (false).

## Web UI

A React + TypeScript app (`src/Trading.Web`, Vite/Tailwind/Zustand) served by the ASP.NET backend
(`src/Trading.Api`). The backend is an MCP client of the data/execution MCP and hosts the crew;
**LLM keys stay server-side** (the UI only selects provider/model). The crew debate streams to the
browser over SSE, and execution submits are guarded by a confirmation.

```bash
# 1) data (+execution) MCP server — see the sections above
# 2) backend (reads TRADING_MCP_URL/bearer + the LLM provider/key from env)
TRADING_MCP_URL=http://127.0.0.1:8080/mcp TRADING_BEARER_TOKEN=dev \
  TRADING_LLM_PROVIDER=anthropic ANTHROPIC_API_KEY=sk-ant-... \
  ASPNETCORE_URLS=http://127.0.0.1:5080 dotnet run --project src/Trading.Api
# 3) web UI (dev server proxies /api -> :5080)
cd src/Trading.Web && npm install && npm run dev   # http://localhost:5175
```

For production the backend serves the built SPA: `npm run build` and copy `src/Trading.Web/dist`
into `src/Trading.Api/wwwroot`. Frontend checks: `npm run lint`, `npm run build`, `npm test`.

## Backlog (in Memory MCP)

```
# active sprint board
notes_search(domain="development", type="backlog_item",
             filter="payload.project == 'binance-maf-trader' AND payload.sprint == 'TRD-S6'",
             includePayload=true)
```

Current sprint **TRD-S6 — React + TypeScript web UI**: a web app (served by the ASP.NET `Trading.Api` backend) that configures a run, streams the crew debate over SSE, shows the decision + risk verdict + balances, and submits (guarded) to the execution MCP; architecture mirrors PFlow. (TRD-S1 data-first, TRD-S2 baseline+backtest, TRD-S3 market-data MCP add-on, TRD-S4 risk+execution, and TRD-S5 the MAF crew are done.)
