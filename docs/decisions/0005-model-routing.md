# 0005 — Model routing across agents (not one model everywhere)

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-model-routing-2026-06-22`

## Decision

Route by task, not one model for every agent: a **cheap/fast model** for extraction and summaries, a **strong default reasoning model** for analyst/debate/trader work, and the **strongest available high-reasoning model only** for ambiguous final trader/risk decisions. Pin and log model IDs and capabilities.

## Context

Trading agents make many repeated calls; using the strongest model everywhere wastes cost and latency. The highest-value calls are synthesis, the trade proposal, and the final risk/portfolio review. Providers change model behavior over time, so the exact IDs and capabilities must be pinned and recorded for reproducible backtests and audits. (As of 2026-06-22, Anthropic's most capable widely released model is Claude Fable 5 and the strongest Opus-tier model is Claude Opus 4.8; verify current IDs at integration time.)

## Consequence

- A model-routing config maps agent role → model tier; IDs are pinned per run and logged with each decision.
- Re-evaluate the routing when providers ship new models.
