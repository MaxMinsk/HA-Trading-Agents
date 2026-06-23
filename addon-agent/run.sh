#!/usr/bin/with-contenv bashio
set -euo pipefail

# Agent/web add-on. Runs where the model APIs are reachable (a local HA). Keys come from add-on
# options (set in the HA UI) and are exported as environment variables for the backend.
export ASPNETCORE_URLS="http://0.0.0.0:8080"
export Logging__LogLevel__Default="$(bashio::config 'log_level')"
export TRADING_MCP_URL="$(bashio::config 'mcp_url')"

if bashio::config.has_value 'bearer_token'; then
  export TRADING_BEARER_TOKEN="$(bashio::config 'bearer_token')"
fi
if bashio::config.has_value 'llm_provider'; then
  export TRADING_LLM_PROVIDER="$(bashio::config 'llm_provider')"
fi
if bashio::config.has_value 'llm_model'; then
  export TRADING_LLM_MODEL="$(bashio::config 'llm_model')"
fi
if bashio::config.has_value 'llm_api_key'; then
  export TRADING_LLM_API_KEY="$(bashio::config 'llm_api_key')"
else
  bashio::log.warning "No llm_api_key set. The crew cannot run until a provider key is configured."
fi

bashio::log.info "Starting Trading Agent (web UI + API on :8080, data MCP at ${TRADING_MCP_URL})"
exec /opt/trading-agent/Trading.Api
