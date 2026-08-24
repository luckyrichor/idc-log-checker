#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd -P)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd -P)"
TOOLS_DIR="$PROJECT_ROOT/.tools"
INSTALLER="$TOOLS_DIR/downloads/dotnet-install.sh"

mkdir -p "$TOOLS_DIR/downloads" "$TOOLS_DIR/dotnet" "$TOOLS_DIR/temp"

if [[ ! -f "$INSTALLER" ]]; then
  curl --fail --location --silent --show-error \
    https://dot.net/v1/dotnet-install.sh \
    --output "$INSTALLER"
fi

bash "$INSTALLER" \
  --channel 10.0 \
  --quality GA \
  --architecture arm64 \
  --install-dir "$TOOLS_DIR/dotnet" \
  --no-path

source "$SCRIPT_DIR/env.sh"
dotnet --info
