# AGENTS.md — Binance MAF Trader

Instructions for agents (and humans) working in this repository. Short version: what the project is, how we work, what not to do.

## What this is

**Binance MAF Trader** is a data-first, low-frequency crypto trading research system. The orchestration layer is **C#/.NET + Microsoft Agent Framework (MAF)**: typed agents and multi-agent workflows (analysts → debate → trader → risk reviewer), with checkpointing, human-in-the-loop, and observability. MAF is the *runtime*, not the edge — the correctness and any real edge come from the **data layer, point-in-time validation, deterministic risk layer, and execution adapter**, which we build first.

Design rationale, alternatives considered, and the research that led here live in the three root research docs (`initial_investigate.md`, `llm_multiagent_trader.md`, `microsoft_agent_framework.md`, in Russian) and in the `binance-maf-trader` project notes in the Memory MCP (`domain=development`).

## How we work — read Memory first

The **backlog, sprints, decisions, and project state live in the Memory MCP**, not in files. Before starting a task:

1. `memory_context(query, domain="development", project="binance-maf-trader")` — rules + skills + relevant notes in one call.
2. The board: `notes_search(domain="development", type="backlog_item", filter="payload.project == 'binance-maf-trader'", includePayload=true)`. Active sprint: `payload.sprint == 'TRD-S1'`; general backlog: `payload.sprint is null`.
3. Save durable decisions/state back to Memory (don't wait to be asked). Prefer `notes_patch` over creating duplicates; never store secrets.

## Language policy

- **Everything tracked by git is in English**: identifiers, XML docs, `README.md`, `AGENTS.md`, ADRs, commit messages.
- The root research docs are Russian working inputs; internal planning (`implementation_plan/`, `Notes~/`) and vendored references (`References~/`, `references/`) are **gitignored**.

## Stack & structure

- **.NET 9** (`global.json` pins the SDK), central package management (`Directory.Packages.props`), analyzers on (`Directory.Build.props` + `.editorconfig`).
- Projects (current + planned):
  - `src/Trading.Core` — storage- and vendor-agnostic domain contracts (candles, point-in-time `MarketSnapshot`, `TradeDecision`, data-source/store/snapshot interfaces). **No runtime dependencies.**
  - `src/Trading.Data` — Binance ingestion (REST history + WebSocket live), canonical store, snapshot builder, data-quality checks. *(TRD-001 — storage choice pending.)*
  - `src/Trading.Agents` — MAF multi-agent workflow. *(TRD-004.)*
  - `src/Trading.Risk` — deterministic risk + execution (sizing, exchange filters, stop/kill-switch, idempotency, testnet/live gating). *(TRD-003.)*
  - `tests/Trading.Tests` — xunit unit + integration tests.
- XML documentation required on public C# types.

## Architectural principles (do not violate)

- **No look-ahead, ever.** A `MarketSnapshot` may contain only data known at or before its decision timestamp. The invariant is enforced in `Trading.Core` (`MarketSnapshot.Create` throws `LookAheadException`) and covered by tests. Backtests that "see the future" are worthless.
- **Data-first.** Build the point-in-time data platform + a deterministic evaluation harness before any multi-agent workflow. Establish baselines (rules + single-agent) first; multi-agent must beat them out-of-sample and in paper trading to earn its complexity.
- **Deterministic code owns risk and execution.** The LLM proposes a `TradeDecision`; plain, testable code validates ranges, sizing, exchange filters, stops, daily-loss limit, kill-switch, and order idempotency, and may veto. No live order path exists without these gates. API keys are withdrawal-disabled, IP-pinned, ideally a sub-account.
- **MAF is orchestration only.** It does not solve market-data quality, point-in-time validation, risk, or execution.
- **Fresh agent/workflow state per decision / per backtest step**, unless deliberately modeling memory — otherwise state leaks between runs and contaminates backtests.
- **Memory MCP is agent memory/audit, not the data lake.** Durable decisions, rationales summaries, rules, lessons, backlog → Memory. Tick/candle/orderbook history → the purpose-built store, never Memory.
- **Low-frequency by design** (1h/4h/daily). LLM cycles are too slow/expensive for scalping/HFT.

## Code quality & conventions

- **One public type per file**, file name == type name; folder-per-feature; file-scoped namespaces; `using`s outside the namespace.
- **Modern C#:** records for data; `required`/primary constructors; collection expressions; pattern matching; raw string literals for SQL. Nullable honored — no unexplained `!`.
- **Async** is async-all-the-way with the `Async` suffix; `CancellationToken` last and flowed down; never `.Result`/`.Wait()`.
- **Keep `main` zero-warning** (CI builds `-warnaserror`); muted analyzer rules carry a rationale in `.editorconfig`.
- **Tests:** `Method_Scenario_Expectation` naming; per-test isolation; deterministic clock via `TimeProvider`; integration tests tagged and separable.

## Backlog conventions

Task key prefix **TRD-NNN** (three+ digits, monotonic, never reused). The backlog is in Memory (`domain=development`, `type=backlog_item`, `payload.project == 'binance-maf-trader'`); sprints are `type=sprint` (e.g. `TRD-S1`). Idempotent by `dedupKey=TRD-NNN`. `payload.status` (`ready`/`next`/`later`/`in_progress`/`done`/`blocked`) is the lifecycle; the envelope `status` is separate. Link tickets with `in_sprint`, `depends_on`, `addresses`.

## Decisions (ADR)

Key forks are recorded in `docs/decisions/` (Decision / Context / Consequence) and mirrored as `type=decision` notes in Memory (`binance-maf-trader-*`). Keep the two in sync.

## Git

No commits or pushes without an explicit request. Branch off the default branch before changes. Commit messages are English and reference the relevant TRD-NNN. Never commit market-data dumps, databases, or API keys (see `.gitignore`).
