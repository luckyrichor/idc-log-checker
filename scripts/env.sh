#!/usr/bin/env bash
set -euo pipefail

if [[ -n "${ZSH_VERSION:-}" ]]; then
  SCRIPT_PATH="${(%):-%x}"
else
  SCRIPT_PATH="${BASH_SOURCE[0]}"
fi
SCRIPT_DIR="$(cd "$(dirname "$SCRIPT_PATH")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"

export DOTNET_ROOT="$PROJECT_ROOT/.tools/dotnet"
export DOTNET_CLI_HOME="$PROJECT_ROOT/.tools/dotnet-home"
export NUGET_PACKAGES="$PROJECT_ROOT/.tools/nuget-packages"
export NUGET_HTTP_CACHE_PATH="$PROJECT_ROOT/.tools/nuget-http-cache"
export TMPDIR="$PROJECT_ROOT/.tools/temp"
export DOTNET_NOLOGO=1
export DOTNET_CLI_TELEMETRY_OPTOUT=1
export DOTNET_SKIP_FIRST_TIME_EXPERIENCE=1
export PATH="$DOTNET_ROOT:$PATH"

mkdir -p "$DOTNET_CLI_HOME" "$NUGET_PACKAGES" "$NUGET_HTTP_CACHE_PATH" "$TMPDIR"
