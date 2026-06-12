#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "$SCRIPT_DIR/../.." && pwd)"

PROJECT="$REPO_ROOT/Manitux.Desktop/Manitux.Desktop.csproj"
OUTPUT_DIR="${1:-$REPO_ROOT/builds/osx-x64-standalone}"
BUILD_ROOT="$REPO_ROOT/builds"

if [[ "$OUTPUT_DIR" == "$BUILD_ROOT"* && -d "$OUTPUT_DIR" ]]; then
  rm -rf "$OUTPUT_DIR"
fi

echo "Publishing standalone osx-x64 build..."
echo "Output: $OUTPUT_DIR"

dotnet publish "$PROJECT" \
  -c Release \
  -r osx-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:UseSharedCompilation=false \
  -maxcpucount:1 \
  -o "$OUTPUT_DIR"

python3 "$SCRIPT_DIR/patch-macho-rpaths.py" "$OUTPUT_DIR/libs"

chmod +x "$OUTPUT_DIR/Manitux.Desktop"

echo "Done."
echo "Run: $OUTPUT_DIR/Manitux.Desktop"
