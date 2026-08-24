#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"

source "$PROJECT_ROOT/scripts/env.sh"
exec "$PROJECT_ROOT/artifacts/macos-arm64-smoke/IDC日志检查工具_Avalonia.app/Contents/MacOS/IDC日志检查工具_Avalonia"
