#!/usr/bin/env bash
# Run the full stack locally without Home Assistant: the data/exec MCP server + the agent web app
# (API serving the built SPA). Configure secrets via env or a .env file at the repo root.
#
#   TRADING_BEARER_TOKEN   bearer shared by the MCP server and the agent (default: devtoken)
#   TRADING_LLM_PROVIDER   anthropic | openai   (required to run the crew)
#   ANTHROPIC_API_KEY / OPENAI_API_KEY / TRADING_LLM_API_KEY   the model key
#   TRADING_EXEC_ENABLED   true to enable paper/testnet execution tools (default: true, paper)
#
# Then open http://127.0.0.1:5080
set -euo pipefail

cd "$(dirname "$0")/.."

if [ -f .env ]; then
  set -a
  # shellcheck disable=SC1091
  . ./.env
  set +a
fi

: "${TRADING_BEARER_TOKEN:=devtoken}"
export TRADING_BEARER_TOKEN

echo "[1/3] Starting data + execution MCP server on :8080 ..."
TRADING_TRANSPORT=http \
  TRADING_DB_PATH="${TRADING_DB_PATH:-data/market.sqlite}" \
  TRADING_INGEST="${TRADING_INGEST:-true}" \
  TRADING_EXEC_ENABLED="${TRADING_EXEC_ENABLED:-true}" \
  TRADING_EXEC_MODE="${TRADING_EXEC_MODE:-paper}" \
  ASPNETCORE_URLS="http://127.0.0.1:8080" \
  dotnet run --project src/Trading.Mcp -c Release &
mcp_pid=$!
trap 'kill "${mcp_pid}" 2>/dev/null || true' EXIT

echo "[2/3] Building the web UI into the API wwwroot ..."
dotnet build src/Trading.Api/Trading.Api.csproj -c Release -p:BuildSpa=true

echo "[3/3] Starting the agent web app on :5080 (open http://127.0.0.1:5080) ..."
TRADING_MCP_URL="http://127.0.0.1:8080/mcp" \
  ASPNETCORE_URLS="http://127.0.0.1:5080" \
  dotnet run --project src/Trading.Api -c Release --no-build
