#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"

PROJECT="$REPO_ROOT/Manitux.Desktop/Manitux.Desktop.csproj"
OUTPUT_DIR="${1:-$REPO_ROOT/builds/linux-x64-standalone}"
HELPER_SOURCE="$REPO_ROOT/Manitux.Desktop/helpers/linux-x64"
HELPER_OUTPUT="$OUTPUT_DIR/libs/helpers"
OUTPUT_PARENT="$(dirname -- "$OUTPUT_DIR")"
OUTPUT_NAME="$(basename -- "$OUTPUT_DIR")"
mkdir -p "$OUTPUT_PARENT"
OUTPUT_REAL="$(cd -- "$OUTPUT_PARENT" && pwd)/$OUTPUT_NAME"

echo "Publishing standalone linux-x64 build..."
echo "Output: $OUTPUT_DIR"

case "$OUTPUT_REAL" in
  "$REPO_ROOT"/builds/*)
    rm -rf "$OUTPUT_REAL"
    ;;
esac

dotnet publish "$PROJECT" \
  -c Release \
  -r linux-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:UseSharedCompilation=false \
  -maxcpucount:1 \
  -o "$OUTPUT_DIR"

if [ ! -d "$HELPER_SOURCE" ]; then
  echo "Missing helper source directory: $HELPER_SOURCE" >&2
  exit 1
fi

mkdir -p "$HELPER_OUTPUT"
cp -a "$HELPER_SOURCE/." "$HELPER_OUTPUT/"

chmod +x "$OUTPUT_DIR/Manitux.Desktop"
if [ -f "$HELPER_OUTPUT/tlsclientapi" ]; then
  chmod +x "$HELPER_OUTPUT/tlsclientapi"
fi
if [ -f "$HELPER_OUTPUT/ytdlp" ]; then
  chmod +x "$HELPER_OUTPUT/ytdlp"
fi

echo "Done."
echo "Run: $OUTPUT_DIR/Manitux.Desktop"
