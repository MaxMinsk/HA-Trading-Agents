# 0007 — Spot-first data layer with a futures-ready schema

**Status:** accepted (2026-06-22)
**Memory:** `decision` note `binance-maf-trader-decision-spot-futures-ready`

## Decision

The data-layer MVP targets Binance **spot**, but the model is **futures-ready** from day one: the `Market` enum includes `UsdmFutures`, the Binance source maps the market to the correct klines base URL (`api.binance.com` vs `fapi.binance.com`), and the store keys candles on `(symbol, market, interval, open_time)`. Shorting and futures-only features (funding rate, open interest, long/short ratios) are a later increment.

## Context

A spot-only MVP is the simplest correct starting point, and spot and USDⓈ-M klines share the same response shape, so adding futures candles is cheap. The user's original goal — profiting in both rising and falling markets — needs futures eventually, but candles, snapshots, and the baseline can be validated on spot first.

## Consequence

- Candle ingestion, the store, snapshots, and the baseline run on spot for the MVP.
- Adding USDⓈ-M futures candles is a base-URL switch; funding/OI/mark-price/long-short ingestion is a separate later increment under TRD-001 / a follow-up ticket.
