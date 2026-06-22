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

bashio::log.info "Starting Trading Data MCP (HTTP on :8080, db=${TRADING_DB_PATH}, symbols=${TRADING_SYMBOLS}, interval=${TRADING_INTERVAL})"
exec /opt/trading-mcp/Trading.Mcp
