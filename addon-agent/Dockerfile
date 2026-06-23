# syntax=docker/dockerfile:1.7

ARG BUILD_FROM=ghcr.io/home-assistant/base:3.23

# 1) Build the React SPA (public npm registry via the committed .npmrc).
FROM --platform=$BUILDPLATFORM node:20-alpine AS web
WORKDIR /web
COPY src/Trading.Web/package.json src/Trading.Web/package-lock.json src/Trading.Web/.npmrc ./
RUN npm ci
COPY src/Trading.Web/ ./
RUN npm run build

# 2) Publish the API self-contained and graft the SPA into wwwroot.
FROM --platform=$BUILDPLATFORM mcr.microsoft.com/dotnet/sdk:9.0-alpine AS build

ARG BUILD_ARCH=amd64
ARG BUILD_VERSION=0.1.0

WORKDIR /src

COPY global.json Directory.Build.props Directory.Packages.props ./
COPY src/Trading.Core/Trading.Core.csproj ./src/Trading.Core/
COPY src/Trading.Agents/Trading.Agents.csproj ./src/Trading.Agents/
COPY src/Trading.Api/Trading.Api.csproj ./src/Trading.Api/

RUN \
    case "${BUILD_ARCH}" in \
      amd64) dotnet_rid="linux-musl-x64" ;; \
      aarch64) dotnet_rid="linux-musl-arm64" ;; \
      *) echo "Unsupported BUILD_ARCH: ${BUILD_ARCH}" && exit 1 ;; \
    esac \
    && dotnet restore ./src/Trading.Api/Trading.Api.csproj --runtime "${dotnet_rid}"

COPY src ./src

RUN \
    case "${BUILD_ARCH}" in \
      amd64) dotnet_rid="linux-musl-x64" ;; \
      aarch64) dotnet_rid="linux-musl-arm64" ;; \
      *) echo "Unsupported BUILD_ARCH: ${BUILD_ARCH}" && exit 1 ;; \
    esac \
    && dotnet publish ./src/Trading.Api/Trading.Api.csproj \
      --configuration Release \
      --runtime "${dotnet_rid}" \
      --self-contained true \
      --no-restore \
      --output /app/publish \
      -p:Version="${BUILD_VERSION}" \
      -p:PublishSingleFile=true \
      -p:PublishTrimmed=false \
      -p:InvariantGlobalization=true

COPY --from=web /web/dist /app/publish/wwwroot

FROM ${BUILD_FROM}

ARG BUILD_ARCH=amd64
ARG BUILD_VERSION=0.1.0

WORKDIR /opt/trading-agent

COPY --from=build /app/publish ./
COPY addon-agent/run.sh /run.sh

RUN chmod a+x /run.sh

LABEL \
    io.hass.version="${BUILD_VERSION}" \
    io.hass.type="app" \
    io.hass.arch="${BUILD_ARCH}" \
    org.opencontainers.image.title="Trading Agent" \
    org.opencontainers.image.description="MAF multi-agent trading crew + web UI (MCP client of the data add-on)." \
    org.opencontainers.image.source="https://github.com/MaxMinsk/HA-Trading-Agents"

CMD [ "/run.sh" ]
