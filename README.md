# Binance MAF Trader

A **data-first, low-frequency** crypto trading research system on **C#/.NET 9 + Microsoft Agent Framework (MAF)**. MAF orchestrates a team of typed agents (analysts → bull/bear debate → trader → risk reviewer); the edge and correctness come from the data, validation, risk, and execution layers underneath — which we build first.

> **Status:** the data layer (with a backtest harness) is exposed over **MCP**, a **MAF multi-agent crew** consumes it, and a **deterministic risk + execution layer** is exposed as MCP write-tools (paper / Binance testnet). `Trading.Core` (contracts + the no-look-ahead snapshot invariant + execution contracts), `Trading.Data` (SQLite store, Binance REST + live WebSocket ingestion, snapshots, data-quality), `Trading.Backtest` (strategies + metrics + buy&hold), `Trading.Risk` (limits + the gate), `Trading.Execution` (execution service + paper & Binance-testnet adapters), `Trading.Agents` (the MAF crew — provider-abstracted over Anthropic + OpenAI), `Trading.Mcp` (the market-data + execution MCP server / HA add-on with in-process ingestion), `Trading.Agent` (a relocatable MCP-client host that runs the crew), `Trading.Api` (an ASP.NET backend), and `Trading.Web` (a React + TypeScript web UI). 116 .NET tests + 20 web tests; `dotnet build -warnaserror` and the web lint/build/test are clean. Both layers install as Home Assistant add-ons (data+execution, and agent/web) and the agent is configurable from the web UI; backtesting the crew against the TRD-002 baselines is next. Backlog, sprints, and decisions live in the **Memory MCP** (`domain=development`, project `binance-maf-trader`), not in this repo.

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
addon/               — HA add-on: data + execution MCP server (Dockerfile, config.yaml, run.sh)
addon-agent/         — HA add-on: agent crew + web UI (MCP client of the data add-on)                     (TRD-S7)
repository.yaml      — HA add-on repository manifest (lists both add-ons; Memory MCP is a separate repo)  (TRD-S7)
scripts/run-local.sh — run the full stack locally (data+exec MCP + agent web app)                         (TRD-S7)
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
(`src/Trading.Api`). The backend is an MCP client of the data/execution MCP and hosts the crew.
Configure the provider/model, the data-MCP URL/bearer and the LLM key on the **Settings** panel —
secrets are stored **server-side** (`data/settings.json`, masked in the UI), or set them via the
add-on options / env. Precedence: **UI settings → env → default**. The crew debate streams to the
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

For production the API serves the built SPA from `wwwroot`: `dotnet build src/Trading.Api -p:BuildSpa=true`
builds the SPA and copies it in (the **Run the full stack locally** section does this for you). Frontend
checks: `npm run lint`, `npm run build`, `npm test`.

## Run the full stack locally

```bash
cp .env.example .env     # set TRADING_BEARER_TOKEN, TRADING_LLM_PROVIDER, ANTHROPIC_API_KEY / OPENAI_API_KEY
./scripts/run-local.sh   # starts the data+exec MCP, builds the SPA into wwwroot, runs the API
# open http://127.0.0.1:5080
```

## Install in Home Assistant

This repository is a Home Assistant **add-on repository** with two add-ons (the Memory MCP is a separate repo).

1. HA → Settings → Add-ons → Add-on Store → ⋮ → **Repositories** → add `https://github.com/MaxMinsk/HA-Trading-Agents`.
2. Install **Trading Data MCP** (data + execution). In its options set `bearer_token`; for execution set `exec_enabled` + the Binance keys (default is paper). Start it.
3. Optionally install **Trading Agent** (crew + web UI) on an HA instance that can reach the model APIs. Set `mcp_url` (the data add-on), `bearer_token` (same), `llm_provider`, `llm_api_key`. Start it and open the Web UI.

Keys can be set in the **add-on options** (or env), or on the web UI **Settings** panel (stored server-side, masked).

## Release

Same model as the Memory MCP add-on: a release is an add-on **version bump merged to `main`**.

1. Bump `version` in `addon/config.yaml` and/or `addon-agent/config.yaml`.
2. Merge to `main`. The `addons` workflow builds the multi-arch images (HA builder actions) and publishes a manifest to GHCR — `ghcr.io/maxminsk/ha-trading-agents-mcp` and `…-agent`, tagged `:<version>` and `:latest`. (Pull requests build the images but do not push; `workflow_dispatch` rebuilds on demand.)
3. In HA Supervisor the add-on shows an update — apply it (Supervisor pulls the new image and restarts the add-on).
4. Restart the local stack on the new version (re-run `scripts/run-local.sh`, or `git pull` + rebuild).

## Smoke test via the UI

1. Open the Web UI — on the **Settings** panel set the provider/model + LLM key and the data-MCP URL/bearer (or rely on env/add-on options), and confirm "crew ready".
2. Pick a symbol/interval and **Run crew** — watch the analyst/bull/bear/trader/risk debate stream in, ending with a decision.
3. Check **Balances** (the paper adapter shows the starting quote when execution is enabled).
4. For a buy/sell decision, **Submit to execution** (confirm) — the paper adapter fills it and the outcome shows the risk verdict.

## Backlog (in Memory MCP)

```
# active sprint board
notes_search(domain="development", type="backlog_item",
             filter="payload.project == 'binance-maf-trader' AND payload.sprint == 'TRD-S7'",
             includePayload=true)
```

Current sprint **TRD-S6 — React + TypeScript web UI**: a web app (served by the ASP.NET `Trading.Api` backend) that configures a run, streams the crew debate over SSE, shows the decision + risk verdict + balances, and submits (guarded) to the execution MCP; architecture mirrors PFlow. (TRD-S1 data-first, TRD-S2 baseline+backtest, TRD-S3 market-data MCP add-on, TRD-S4 risk+execution, and TRD-S5 the MAF crew are done.)
