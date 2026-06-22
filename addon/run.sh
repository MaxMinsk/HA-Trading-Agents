#!/usr/bin/with-contenv bashio
set -euo pipefail

export TRADING_TRANSPORT="http"
export TRADING_DB_PATH="$(bashio::config 'db_path')"
export ASPNETCORE_URLS="http://0.0.0.0:8080"
export Logging__LogLevel__Default="$(bashio::config 'log_level')"

# In-add-on ingestion (backfill + live WebSocket) keeps the store fresh.
export TRADING_INGEST="true"
export TRADING_SYMBOLS="$(bashio::config 'symbols')"
export TRADING_INTERVAL="$(bashio::config 'interval')"
export TRADING_BACKFILL_DAYS="$(bashio::config 'backfill_days')"
export TRADING_SPOT_KLINES_URL="$(bashio::config 'spot_klines_url')"

if bashio::config.has_value 'bearer_token'; then
  export TRADING_BEARER_TOKEN="$(bashio::config 'bearer_token')"
else
  bashio::log.fatal "No bearer_token set. The HTTP MCP endpoint refuses to start unauthenticated — set 'bearer_token' in the add-on options."
  exit 1
fi

# Execution layer (off by default; keys stay on the server). Risk gate runs server-side.
export TRADING_EXEC_ENABLED="$(bashio::config 'exec_enabled')"
if bashio::config.true 'exec_enabled'; then
  export TRADING_EXEC_MODE="$(bashio::config 'exec_mode')"
  export TRADING_PAPER_QUOTE="$(bashio::config 'paper_quote')"
  export TRADING_BINANCE_LIVE="$(bashio::config 'binance_live')"
  export TRADING_MAX_POSITION_FRACTION="$(bashio::config 'max_position_fraction')"
  export TRADING_MAX_ORDER_NOTIONAL="$(bashio::config 'max_order_notional')"
  export TRADING_DAILY_LOSS_FRACTION="$(bashio::config 'daily_loss_fraction')"
  export TRADING_KILL_SWITCH="$(bashio::config 'kill_switch')"
  if bashio::config.has_value 'binance_api_key'; then
    export TRADING_BINANCE_API_KEY="$(bashio::config 'binance_api_key')"
  fi
  if bashio::config.has_value 'binance_api_secret'; then
    export TRADING_BINANCE_API_SECRET="$(bashio::config 'binance_api_secret')"
  fi
  if bashio::config.true 'binance_live'; then
    bashio::log.warning "Execution is in LIVE (mainnet) mode — real funds at risk."
  fi
  bashio::log.info "Execution enabled (mode=${TRADING_EXEC_MODE}, live=$(bashio::config 'binance_live'))."
fi

bashio::log.info "Starting Trading Data MCP (HTTP on :8080, db=${TRADING_DB_PATH}, symbols=${TRADING_SYMBOLS}, interval=${TRADING_INTERVAL})"
exec /opt/trading-mcp/Trading.Mcp
