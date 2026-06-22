# 0004 — Borrow TradingAgents patterns, not the Python runtime

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-tradingagents-reference-not-dependency`

## Decision

Use **tauricresearch/tradingagents** (v0.3.0) as a **reference** for workflow shape, data contracts, testing, memory/reflection, and provider registries. Do **not** take it as a direct dependency.

## Context

The repo encodes good anti-hallucination / data-quality patterns around agentic trading research, but its implementation is Python / LangGraph / yfinance / stock-oriented and does not match the C#/.NET + MAF + Binance stack. Port the invariants, not the code. (Cloned for reference to `References~/tradingagents`, gitignored.)

## Consequence

- We re-implement the valuable contracts/tests/patterns in C# (TRD-005), adapted to MAF and Binance.
- No Python runtime dependency in the production system.
