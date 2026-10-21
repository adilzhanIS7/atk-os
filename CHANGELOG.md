# Changelog

## 1.0.0 — 2026-10-21
- Windows installer (Inno Setup): per-user install without admin rights, Start menu entry,
  optional autostart and desktop shortcut, English and Russian, clean uninstall
- Linux `.deb` package with desktop entry, icon and udev rule; macOS `.dmg` with `ATK-OS.app`
- Portable Windows zip and Linux tarball are still available
- CI builds and test-installs every package on every push
- Version and GitHub link in the window; fixed blue accent instead of the system one

## 0.1.1 — 2026-10-19
- App icon
- Only one copy runs at a time; launching the exe again opens the running copy's window
- File description, version and copyright in the Windows executables
- `SHA256SUMS.txt` in releases

## 0.1.0 — 2026-10-19
- First release: tray app with battery level, low-battery popup, autostart
- DPI levels, values and colors, polling rate, lift-off distance, debounce,
  motion sync, angle snapping, ripple control
- `atkctl` command-line tool
- Windows, Linux and macOS builds (native AOT)
