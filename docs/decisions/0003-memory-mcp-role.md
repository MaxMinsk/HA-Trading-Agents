# 0003 — Memory MCP is agent memory/audit, not the market data store

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-memory-mcp-role`

## Decision

Use the **Memory MCP** for the trading agent's long-term memory, audit trail, decisions, backlog, and run summaries. **Do not** use it as the market data lake or backtest database.

## Context

Memory MCP is optimized for durable typed notes (facts/decisions/backlog), full-text search, graph links, and agent recall. Binance market data is high-volume, temporal, and requires exact point-in-time reconstruction — a different storage problem.

## Consequence

- Durable decisions, rationale summaries, rules, lessons, project state, and the TRD-NNN backlog live in Memory (`domain=development`, project `binance-maf-trader`).
- Tick/candle/orderbook history lives in the purpose-built store in `Trading.Data` (choice pending TRD-001).
