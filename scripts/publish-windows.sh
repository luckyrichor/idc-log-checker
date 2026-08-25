#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
PROJECT_ROOT="$(cd "$SCRIPT_DIR/.." && pwd)"
ARTIFACTS="$PROJECT_ROOT/artifacts/windows"
RELEASE_DIR="$PROJECT_ROOT/发布版"

source "$PROJECT_ROOT/scripts/env.sh"
mkdir -p "$ARTIFACTS/avalonia" "$ARTIFACTS/winforms" "$RELEASE_DIR"

dotnet publish "$PROJECT_ROOT/src/Checker.Avalonia/Checker.Avalonia.csproj" \
  --configuration Release --runtime win-x64 --self-contained true \
  --output "$ARTIFACTS/avalonia" \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false

dotnet publish "$PROJECT_ROOT/src/Checker.WinForms/Checker.WinForms.csproj" \
  --configuration Release --runtime win-x64 --self-contained true \
  --output "$ARTIFACTS/winforms" \
  -p:PublishSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None -p:DebugSymbols=false -p:PublishTrimmed=false

cp "$ARTIFACTS/avalonia/IDC日志检查工具_Avalonia.exe" "$RELEASE_DIR/IDC日志检查工具_Avalonia.exe"
cp "$ARTIFACTS/winforms/IDC日志检查工具_WinForms.exe" "$RELEASE_DIR/IDC日志检查工具_WinForms.exe"
python3 "$PROJECT_ROOT/scripts/write-release-checksums.py" "$RELEASE_DIR"

echo "Windows 单文件程序已生成到：$RELEASE_DIR"
