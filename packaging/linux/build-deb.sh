#!/usr/bin/env bash
# Builds atk-os_<version>_amd64.deb from a publish directory.
# Usage: packaging/linux/build-deb.sh <version> <publish-dir> <output-dir>
set -euo pipefail
VERSION="$1"; SRC="$2"; OUT="$3"
HERE="$(cd "$(dirname "$0")" && pwd)"; ROOT="$HERE/../.."
PKG="$(mktemp -d)/atk-os_${VERSION}_amd64"

install -d "$PKG/DEBIAN" "$PKG/opt/atk-os" "$PKG/usr/bin" \
  "$PKG/usr/share/applications" "$PKG/usr/share/icons/hicolor/scalable/apps" \
  "$PKG/usr/share/icons/hicolor/256x256/apps" "$PKG/usr/lib/udev/rules.d" \
  "$PKG/usr/share/doc/atk-os"

install -m 755 "$SRC/atk-os" "$SRC/atkctl" "$PKG/opt/atk-os/"
install -m 644 "$SRC"/*.so "$PKG/opt/atk-os/"
ln -s /opt/atk-os/atk-os "$PKG/usr/bin/atk-os"
ln -s /opt/atk-os/atkctl "$PKG/usr/bin/atkctl"
install -m 644 "$HERE/atk-os.desktop" "$PKG/usr/share/applications/"
install -m 644 "$ROOT/assets/icon.svg" "$PKG/usr/share/icons/hicolor/scalable/apps/atk-os.svg"
install -m 644 "$ROOT/src/AtkOs.App/Assets/icon.png" "$PKG/usr/share/icons/hicolor/256x256/apps/atk-os.png"
install -m 644 "$HERE/70-atk-os.rules" "$PKG/usr/lib/udev/rules.d/"
install -m 644 "$ROOT/LICENSE" "$PKG/usr/share/doc/atk-os/copyright"
install -m 644 "$ROOT/README.md" "$PKG/usr/share/doc/atk-os/"

SIZE_KB=$(du -sk "$PKG" | cut -f1)
cat > "$PKG/DEBIAN/control" <<CONTROL
Package: atk-os
Version: $VERSION
Section: utils
Priority: optional
Architecture: amd64
Installed-Size: $SIZE_KB
Depends: libc6 (>= 2.27), libx11-6, libice6, libsm6, libfontconfig1
Maintainer: Adilzhan <noreply@github.com>
Homepage: https://github.com/adilzhanIS7/atk-os
Description: Lightweight settings app for ATK / VXE / VGN mice
 Unofficial open-source alternative to ATK V HUB: tray app with battery
 level, DPI, polling rate, lift-off distance and other mouse settings.
 No telemetry, no network. Includes the atkctl command-line tool.
CONTROL

# Let the logged-in user talk to the mouse without root.
cat > "$PKG/DEBIAN/postinst" <<'POSTINST'
#!/bin/sh
set -e
if command -v udevadm >/dev/null 2>&1; then
  udevadm control --reload-rules || true
  udevadm trigger --subsystem-match=hidraw || true
fi
if command -v update-desktop-database >/dev/null 2>&1; then update-desktop-database -q || true; fi
if command -v gtk-update-icon-cache >/dev/null 2>&1; then gtk-update-icon-cache -q -t /usr/share/icons/hicolor || true; fi
POSTINST
cat > "$PKG/DEBIAN/postrm" <<'POSTRM'
#!/bin/sh
set -e
if command -v udevadm >/dev/null 2>&1; then udevadm control --reload-rules || true; fi
POSTRM
chmod 755 "$PKG/DEBIAN/postinst" "$PKG/DEBIAN/postrm"

mkdir -p "$OUT"
dpkg-deb --build --root-owner-group "$PKG" "$OUT/atk-os_${VERSION}_amd64.deb"
