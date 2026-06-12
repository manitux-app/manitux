#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"

PROJECT="$REPO_ROOT/Manitux.Desktop/Manitux.Desktop.csproj"
RUNTIME_ID="osx-arm64"
VERSION="$(dotnet msbuild "$PROJECT" -getProperty:Version -nologo | tr -d '\r' | tail -n 1)"
VERSION="${VERSION:-0.0.0}"
ASSET_NAME="Manitux_${RUNTIME_ID}_v${VERSION}"
OUTPUT_DIR="${1:-$REPO_ROOT/builds/$ASSET_NAME}"
ASSET_ZIP="${2:-$REPO_ROOT/builds/$ASSET_NAME.zip}"
OUTPUT_PARENT="$(dirname -- "$OUTPUT_DIR")"
OUTPUT_NAME="$(basename -- "$OUTPUT_DIR")"
ZIP_PARENT="$(dirname -- "$ASSET_ZIP")"
mkdir -p "$OUTPUT_PARENT" "$ZIP_PARENT"
OUTPUT_REAL="$(cd -- "$OUTPUT_PARENT" && pwd)/$OUTPUT_NAME"
ZIP_REAL="$(cd -- "$ZIP_PARENT" && pwd)/$(basename -- "$ASSET_ZIP")"

echo "Publishing standalone $RUNTIME_ID build..."
echo "Output: $OUTPUT_REAL"
echo "Asset: $ZIP_REAL"

case "$OUTPUT_REAL" in
  "$REPO_ROOT"/builds/*)
    rm -rf "$OUTPUT_REAL"
    ;;
esac

dotnet publish "$PROJECT" \
  -c Release \
  -r "$RUNTIME_ID" \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -p:DebugType=None \
  -p:DebugSymbols=false \
  -p:UseSharedCompilation=false \
  -maxcpucount:1 \
  -o "$OUTPUT_REAL"

python3 "$SCRIPT_DIR/patch-macho-rpaths.py" "$OUTPUT_REAL/libs"

chmod +x "$OUTPUT_REAL/Manitux.Desktop"
rm -f "$ZIP_REAL"
(cd "$OUTPUT_REAL" && zip -qr "$ZIP_REAL" .)

echo "Done."
echo "Run: $OUTPUT_REAL/Manitux.Desktop"
echo "Release asset: $ZIP_REAL"
