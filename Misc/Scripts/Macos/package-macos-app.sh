#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../../.." && pwd)"

PROJECT="$REPO_ROOT/Manitux.Desktop/Manitux.Desktop.csproj"
APP_NAME="Manitux"
EXECUTABLE_NAME="Manitux.Desktop"
ICON_SOURCE="$REPO_ROOT/Manitux/Assets/icons/Manitux.icns"
ICON_FILE="Manitux.icns"
RUNTIME_ID="${1:-osx-arm64}"

case "$RUNTIME_ID" in
  osx-arm64|osx-x64) ;;
  *)
    echo "usage: $0 [osx-arm64|osx-x64] [distribution-dir] [zip-path] [dmg-path]" >&2
    exit 2
    ;;
esac

VERSION="$(dotnet msbuild "$PROJECT" -getProperty:Version -nologo | tr -d '\r' | tail -n 1)"
VERSION="${VERSION:-0.0.0}"
ASSET_NAME="Manitux_${RUNTIME_ID}_v${VERSION}"
BUILDS_ROOT="$REPO_ROOT/builds"
PUBLISH_DIR="$BUILDS_ROOT/.macos-publish/$ASSET_NAME"
DIST_DIR="${2:-$BUILDS_ROOT/$ASSET_NAME}"
ASSET_ZIP="${3:-$BUILDS_ROOT/$ASSET_NAME.zip}"
ASSET_DMG="${4:-$BUILDS_ROOT/$ASSET_NAME.dmg}"

DIST_PARENT="$(dirname -- "$DIST_DIR")"
DIST_NAME="$(basename -- "$DIST_DIR")"
ZIP_PARENT="$(dirname -- "$ASSET_ZIP")"
DMG_PARENT="$(dirname -- "$ASSET_DMG")"
mkdir -p "$DIST_PARENT" "$ZIP_PARENT" "$DMG_PARENT" "$BUILDS_ROOT/.macos-publish"

DIST_REAL="$(cd -- "$DIST_PARENT" && pwd)/$DIST_NAME"
ZIP_REAL="$(cd -- "$ZIP_PARENT" && pwd)/$(basename -- "$ASSET_ZIP")"
DMG_REAL="$(cd -- "$DMG_PARENT" && pwd)/$(basename -- "$ASSET_DMG")"

APP_DIR="$DIST_REAL/$APP_NAME.app"
CONTENTS_DIR="$APP_DIR/Contents"
MACOS_DIR="$CONTENTS_DIR/MacOS"
FRAMEWORKS_DIR="$CONTENTS_DIR/Frameworks"
HELPERS_DIR="$CONTENTS_DIR/Helpers"
RESOURCES_DIR="$CONTENTS_DIR/Resources"

echo "Publishing standalone $RUNTIME_ID build..."
echo "Distribution: $DIST_REAL"
echo "App bundle  : $APP_DIR"
echo "Zip asset   : $ZIP_REAL"
echo "DMG asset   : $DMG_REAL"

case "$DIST_REAL" in
  "$BUILDS_ROOT"/*) rm -rf "$DIST_REAL" ;;
  *) echo "Refusing to remove distribution outside builds: $DIST_REAL" >&2; exit 1 ;;
esac
case "$PUBLISH_DIR" in
  "$BUILDS_ROOT"/.macos-publish/*) rm -rf "$PUBLISH_DIR" ;;
  *) echo "Refusing to remove publish directory outside builds: $PUBLISH_DIR" >&2; exit 1 ;;
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
  -o "$PUBLISH_DIR"

mkdir -p "$MACOS_DIR" "$FRAMEWORKS_DIR" "$HELPERS_DIR" "$RESOURCES_DIR"

if command -v rsync >/dev/null 2>&1; then
  rsync -a --exclude '/libs' "$PUBLISH_DIR"/ "$MACOS_DIR"/
else
  (cd "$PUBLISH_DIR" && tar --exclude './libs' -cf - .) | (cd "$MACOS_DIR" && tar -xf -)
fi

if compgen -G "$PUBLISH_DIR/libs/*.dylib" >/dev/null; then
  cp -a "$PUBLISH_DIR"/libs/*.dylib "$FRAMEWORKS_DIR"/
fi

if [ -d "$PUBLISH_DIR/libs/helpers" ]; then
  cp -a "$PUBLISH_DIR"/libs/helpers/. "$HELPERS_DIR"/
fi

if [ -f "$ICON_SOURCE" ]; then
  cp -a "$ICON_SOURCE" "$RESOURCES_DIR/$ICON_FILE"
else
  echo "Missing macOS icon: $ICON_SOURCE" >&2
  exit 1
fi

if [ -f "$SCRIPT_DIR/../patch-macho-rpaths.py" ] && compgen -G "$FRAMEWORKS_DIR/*.dylib" >/dev/null; then
  python3 "$SCRIPT_DIR/../patch-macho-rpaths.py" "$FRAMEWORKS_DIR"
fi

if command -v install_name_tool >/dev/null 2>&1 && [ -f "$MACOS_DIR/$EXECUTABLE_NAME" ]; then
  install_name_tool -add_rpath "@executable_path/../Frameworks" "$MACOS_DIR/$EXECUTABLE_NAME" 2>/dev/null || true
fi

chmod +x "$MACOS_DIR/$EXECUTABLE_NAME"
if [ -d "$HELPERS_DIR" ]; then
  find "$HELPERS_DIR" -type f -exec chmod +x {} \;
fi

cat > "$CONTENTS_DIR/Info.plist" <<PLIST
<?xml version="1.0" encoding="UTF-8"?>
<!DOCTYPE plist PUBLIC "-//Apple//DTD PLIST 1.0//EN" "http://www.apple.com/DTDs/PropertyList-1.0.dtd">
<plist version="1.0">
<dict>
  <key>CFBundleDevelopmentRegion</key>
  <string>en</string>
  <key>CFBundleExecutable</key>
  <string>$EXECUTABLE_NAME</string>
  <key>CFBundleIdentifier</key>
  <string>app.manitux.desktop</string>
  <key>CFBundleInfoDictionaryVersion</key>
  <string>6.0</string>
  <key>CFBundleName</key>
  <string>$APP_NAME</string>
  <key>CFBundleDisplayName</key>
  <string>$APP_NAME</string>
  <key>CFBundlePackageType</key>
  <string>APPL</string>
  <key>CFBundleIconFile</key>
  <string>$ICON_FILE</string>
  <key>CFBundleShortVersionString</key>
  <string>$VERSION</string>
  <key>CFBundleVersion</key>
  <string>$VERSION</string>
  <key>LSMinimumSystemVersion</key>
  <string>10.15</string>
  <key>NSHighResolutionCapable</key>
  <true/>
</dict>
</plist>
PLIST

printf "APPL????" > "$CONTENTS_DIR/PkgInfo"

cat > "$DIST_REAL/install.sh" <<'INSTALL'
#!/usr/bin/env bash
set -euo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
APP_NAME="Manitux.app"
SOURCE_APP="$SCRIPT_DIR/$APP_NAME"
DEST_DIR="${1:-/Applications}"
DEST_APP="$DEST_DIR/$APP_NAME"

if [ ! -d "$SOURCE_APP" ]; then
  echo "Missing app bundle: $SOURCE_APP" >&2
  exit 1
fi

clear_app_attributes() {
  local app_path="$1"
  xattr -cr "$app_path" 2>/dev/null || true
  xattr -dr com.apple.quarantine "$app_path" 2>/dev/null || true
  xattr -dr com.apple.provenance "$app_path" 2>/dev/null || true
}

fix_app_permissions() {
  local app_path="$1"
  chmod -R u+rwX,go+rX "$app_path"
  chmod +x "$app_path/Contents/MacOS/Manitux.Desktop"

  if [ -d "$app_path/Contents/Helpers" ]; then
    find "$app_path/Contents/Helpers" -type f -exec chmod +x {} \;
  fi

  if [ -d "$app_path/Contents/Frameworks" ]; then
    find "$app_path/Contents/Frameworks" -type f -name "*.dylib" -exec chmod +x {} \;
  fi
}

sign_app() {
  local app_path="$1"
  if command -v codesign >/dev/null 2>&1; then
    codesign --force --deep --sign - "$app_path"
  fi
}

prepare_app() {
  local app_path="$1"
  clear_app_attributes "$app_path"
  fix_app_permissions "$app_path"
  sign_app "$app_path"
  clear_app_attributes "$app_path"
}

prepare_app "$SOURCE_APP"

copy_app() {
  rm -rf "$DEST_APP"
  ditto "$SOURCE_APP" "$DEST_APP"
  prepare_app "$DEST_APP"
}

if [ -w "$DEST_DIR" ]; then
  copy_app
else
  sudo mkdir -p "$DEST_DIR"
  sudo rm -rf "$DEST_APP"
  sudo ditto "$SOURCE_APP" "$DEST_APP"
  sudo xattr -cr "$DEST_APP" 2>/dev/null || true
  sudo xattr -dr com.apple.quarantine "$DEST_APP" 2>/dev/null || true
  sudo xattr -dr com.apple.provenance "$DEST_APP" 2>/dev/null || true
  sudo chmod -R u+rwX,go+rX "$DEST_APP"
  sudo chmod +x "$DEST_APP/Contents/MacOS/Manitux.Desktop"
  if [ -d "$DEST_APP/Contents/Helpers" ]; then
    sudo find "$DEST_APP/Contents/Helpers" -type f -exec chmod +x {} \;
  fi
  if [ -d "$DEST_APP/Contents/Frameworks" ]; then
    sudo find "$DEST_APP/Contents/Frameworks" -type f -name "*.dylib" -exec chmod +x {} \;
  fi
  if command -v codesign >/dev/null 2>&1; then
    sudo codesign --force --deep --sign - "$DEST_APP"
  fi
  sudo xattr -cr "$DEST_APP" 2>/dev/null || true
  sudo xattr -dr com.apple.quarantine "$DEST_APP" 2>/dev/null || true
  sudo xattr -dr com.apple.provenance "$DEST_APP" 2>/dev/null || true
fi

echo "Installed: $DEST_APP"
INSTALL
chmod +x "$DIST_REAL/install.sh"

if command -v codesign >/dev/null 2>&1; then
  codesign --force --deep --sign - "$APP_DIR" || true
fi

rm -f "$ZIP_REAL"
if command -v ditto >/dev/null 2>&1; then
  ditto -c -k --sequesterRsrc --keepParent "$APP_DIR" "$ZIP_REAL"
else
  (cd "$DIST_REAL" && zip -qr "$ZIP_REAL" "$APP_NAME.app")
fi

if command -v hdiutil >/dev/null 2>&1; then
  rm -f "$DMG_REAL"
  hdiutil create -volname "$APP_NAME" -srcfolder "$DIST_REAL" -ov -format UDZO "$DMG_REAL"
elif command -v genisoimage >/dev/null 2>&1; then
  rm -f "$DMG_REAL"
  genisoimage \
    -quiet \
    -V "$APP_NAME" \
    -R \
    -J \
    -joliet-long \
    -D \
    -hfs \
    -mac-name \
    -hfs-volid "$APP_NAME" \
    -hfs-type TEXT \
    -hfs-creator ttxt \
    -o "$DMG_REAL" \
    "$DIST_REAL"
else
  echo "hdiutil/genisoimage not found; skipped DMG creation."
fi

echo "Done."
echo "Install script: $DIST_REAL/install.sh"
echo "App bundle    : $APP_DIR"
echo "Zip asset     : $ZIP_REAL"
if [ -f "$DMG_REAL" ]; then
  echo "DMG asset     : $DMG_REAL"
fi
