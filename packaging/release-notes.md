First public release of ATK-OS — a lightweight, open-source alternative to ATK V HUB.
Unofficial project, not affiliated with ATK, VXE or VGN.

### What it does
- Battery level on the tray icon, low-battery popup
- DPI levels (values, count, active level, indicator colors)
- Polling rate, lift-off distance, button debounce
- Motion sync, angle snapping, ripple control
- Start with system (in tray)
- `atkctl` command-line tool

Every change is read back from the mouse to make sure it was stored. Firmware updates are not supported.

### Downloads
| System | File |
|---|---|
| Windows 10/11 x64 | `atk-os-*-win-x64.zip` → run `atk-os.exe` |
| Linux x64 | `atk-os-*-linux-x64.tar.gz` → install `70-atk-os.rules` (see README), run `atk-os` |
| macOS (Apple Silicon) | `atk-os-*-osx-arm64.tar.gz` → run `atk-os` |

No .NET installation needed: the apps are native executables.

**macOS:** the build is not signed. If macOS refuses to open it, run
`xattr -dr com.apple.quarantine <folder>` once.

### Tested hardware
- VGN Dragonfly R1 SE+ (2.4 GHz receiver `373B:1085`) on Windows 11

Other Compx-based mice should work. Linux and macOS builds have not been tested
with a real mouse yet. Please open an issue with the output of `atkctl info -v`.
