# 0001 — Stack: C#/.NET + Microsoft Agent Framework, built data-first and low-frequency

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-csharp-maf-data-first`

## Decision

Use **C#/.NET + Microsoft Agent Framework (MAF)** for orchestration, with a **data-first, low-frequency** Binance trader design.

## Context

The user prefers C# and MAF. MAF is a good orchestrator for typed multi-agent workflows, human-in-the-loop, checkpointing, and observability — but it does **not** solve market-data quality, point-in-time validation, risk control, or execution correctness. Those layers determine whether the system is useful. LLM multi-agent cycles are too slow and too expensive for scalping/HFT, but are useful for synthesizing market data, news/sentiment, and risk narratives at 1h/4h/daily cadence.

## Consequence

- MAF is treated as the runtime/orchestration layer only.
- The first milestones are the data platform, the evaluation harness, the deterministic risk layer, and the execution adapter — not the agent graph.
- Decisions run at low frequency.
