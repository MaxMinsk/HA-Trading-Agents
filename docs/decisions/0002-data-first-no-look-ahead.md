# 0002 — Data-first foundation and the no-look-ahead invariant

**Status:** accepted (2026-06-22)
**Memory:** `reference` note `binance-maf-trader-architecture-analysis-2026-06-22`

## Decision

Build a **point-in-time market-data platform and a deterministic evaluation harness before** any complex multi-agent workflow. Enforce the **no-look-ahead invariant in code**: a `MarketSnapshot` may only contain data known at or before its decision timestamp.

## Context

The dominant failure modes of ML/agentic trading are overfitting and **data leakage / look-ahead** (including an LLM "knowing" historical outcomes from training). A backtest that sees the future is worse than useless — it manufactures false confidence. Edge, if any, must be demonstrated out-of-sample and in paper trading against baselines, with fees and slippage modeled.

## Consequence

- `Trading.Core.MarketSnapshot.Create` validates and throws `LookAheadException`; tests cover it.
- Recommended architecture: ingestion → append-only canonical store (keyed by source/symbol/market/event_time/ingested_at/version) → point-in-time snapshot builder → evaluation harness (rules + single-agent baseline first, then MAF multi-agent) → deterministic risk/execution → Memory MCP for audit.
- Each decision / backtest step uses fresh agent/workflow state to prevent cross-run contamination.
