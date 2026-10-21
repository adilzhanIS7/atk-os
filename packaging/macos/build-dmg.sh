#!/usr/bin/env bash
# Builds ATK-OS.app and ATK-OS-<version>-macos-arm64.dmg from a publish directory (run on macOS).
# Usage: packaging/macos/build-dmg.sh <version> <publish-dir> <output-dir>
set -euo pipefail
VERSION="$1"; SRC="$2"; OUT="$3"
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$HERE/../.."
WORK="$(mktemp -d)"; APP="$WORK/dmg/ATK-OS.app"

mkdir -p "$APP/Contents/MacOS" "$APP/Contents/Resources"
cp "$SRC/atk-os" "$SRC/atkctl" "$SRC"/*.dylib "$APP/Contents/MacOS/"
sed "s/@VERSION@/$VERSION/g" "$HERE/Info.plist" > "$APP/Contents/Info.plist"

# Icon: every size macOS asks for, from the 1024 px master.
ICONSET="$WORK/atk-os.iconset"; mkdir -p "$ICONSET"
for s in 16 32 128 256 512; do
  sips -z $s $s "$ROOT/assets/icon.png" --out "$ICONSET/icon_${s}x${s}.png" >/dev/null
  sips -z $((s*2)) $((s*2)) "$ROOT/assets/icon.png" --out "$ICONSET/icon_${s}x${s}@2x.png" >/dev/null
done
iconutil -c icns "$ICONSET" -o "$APP/Contents/Resources/atk-os.icns"

# Ad-hoc signature (required on Apple Silicon after the bundle is assembled).
codesign --force --deep --sign - "$APP"

ln -s /Applications "$WORK/dmg/Applications"
cp "$ROOT/LICENSE" "$WORK/dmg/LICENSE.txt"
mkdir -p "$OUT"
hdiutil create -volname "ATK-OS $VERSION" -srcfolder "$WORK/dmg" -ov -format UDZO \
  "$OUT/ATK-OS-$VERSION-macos-arm64.dmg"
