# syntax=docker/dockerfile:1.7

ARG BUILD_FROM=ghcr.io/home-assistant/base:3.23

FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG BUILD_ARCH=amd64
ARG BUILD_VERSION=0.1.0

WORKDIR /src

# Restore layer: copy only the project graph + central config first for caching.
COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Trading.Core/Trading.Core.csproj ./src/Trading.Core/
COPY src/Trading.Data/Trading.Data.csproj ./src/Trading.Data/
COPY src/Trading.Risk/Trading.Risk.csproj ./src/Trading.Risk/
COPY src/Trading.Execution/Trading.Execution.csproj ./src/Trading.Execution/
COPY src/Trading.Mcp/Trading.Mcp.csproj ./src/Trading.Mcp/

RUN \
    case "${BUILD_ARCH}" in \
      amd64) dotnet_rid="linux-musl-x64" ;; \
      aarch64) dotnet_rid="linux-musl-arm64" ;; \
      *) echo "Unsupported BUILD_ARCH: ${BUILD_ARCH}" && exit 1 ;; \
    esac \
    && dotnet restore ./src/Trading.Mcp/Trading.Mcp.csproj --runtime "${dotnet_rid}"

COPY src ./src

RUN \
    case "${BUILD_ARCH}" in \
      amd64) dotnet_rid="linux-musl-x64" ;; \
      aarch64) dotnet_rid="linux-musl-arm64" ;; \
      *) echo "Unsupported BUILD_ARCH: ${BUILD_ARCH}" && exit 1 ;; \
    esac \
    && dotnet publish ./src/Trading.Mcp/Trading.Mcp.csproj \
      --configuration Release \
      --runtime "${dotnet_rid}" \
      --self-contained true \
      --no-restore \
      --output /app/publish \
      -p:Version="${BUILD_VERSION}" \
      -p:PublishSingleFile=true \
      -p:PublishTrimmed=false \
      -p:InvariantGlobalization=true

FROM ${BUILD_FROM}

ARG BUILD_ARCH=amd64
ARG BUILD_VERSION=0.1.0

WORKDIR /opt/trading-mcp

COPY --from=build /app/publish ./
COPY addon/run.sh /run.sh

RUN chmod a+x /run.sh

LABEL \
    io.hass.version="${BUILD_VERSION}" \
    io.hass.type="app" \
    io.hass.arch="${BUILD_ARCH}" \
    org.opencontainers.image.title="Trading Data MCP" \
    org.opencontainers.image.description="Binance market-data layer exposed over MCP for trading agents." \
    org.opencontainers.image.source="https://github.com/MaxMinsk/HA-Trading-Agents"

CMD [ "/run.sh" ]
