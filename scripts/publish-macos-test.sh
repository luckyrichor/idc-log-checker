#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
OUTPUT="$PROJECT_ROOT/artifacts/macos-arm64-smoke"
APP_DIR="$OUTPUT/IDC日志检查工具_Avalonia.app"
RUNTIME_OUTPUT="$APP_DIR/Contents/MacOS"

source "$PROJECT_ROOT/scripts/env.sh"
mkdir -p "$RUNTIME_OUTPUT"
dotnet publish "$PROJECT_ROOT/src/Checker.Avalonia/Checker.Avalonia.csproj" \
  --configuration Release --runtime osx-arm64 --self-contained true \
  --output "$RUNTIME_OUTPUT" \
  -p:PublishSingleFile=false \
  -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false

cp "$PROJECT_ROOT/scripts/macos-smoke/Info.plist" "$APP_DIR/Contents/Info.plist"

echo "macOS 测试应用已生成到：$APP_DIR"
