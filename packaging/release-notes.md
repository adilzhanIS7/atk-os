ATK-OS is a lightweight, open-source settings app for ATK / VXE / VGN gaming mice: an alternative to the official ATK V HUB.
~15 MB instead of ~430 MB, about 5 MB of memory in the tray, no telemetry.
Unofficial project, not affiliated with ATK, VXE or VGN.

### Downloads
| System | File |
|---|---|
| **Windows 10/11** | `ATK-OS-Setup-*-x64.exe` (installer) or `atk-os-*-win-x64-portable.zip` |
| **Linux** (Ubuntu / Debian / Mint) | `atk-os_*_amd64.deb` → `sudo apt install ./atk-os_*.deb` |
| **Linux** (other) | `atk-os-*-linux-x64.tar.gz` |
| **macOS 11+** (Apple Silicon) | `ATK-OS-*-macos-arm64.dmg` |

No .NET installation needed: the apps are native executables. Checksums: `SHA256SUMS.txt`.

### What's new in 1.0
- Installers: Windows setup (Start menu, autostart, uninstaller, no admin rights needed), Linux `.deb` with the udev rule, macOS `.dmg` with `ATK-OS.app`
- Version and project link in the window, consistent blue theme
- Everything from 0.1: battery on the tray icon, low-battery popup, DPI levels and colors, polling rate, lift-off distance, debounce, motion sync, angle snapping, ripple control, `atkctl` CLI, one running copy at a time

### First launch
- **Windows:** the build is not code-signed yet. If SmartScreen says "Windows protected your PC", click **More info → Run anyway**.
- **macOS:** the app is not notarized. Right-click ATK-OS in Applications → **Open** (once).

### Tested hardware
- VGN Dragonfly R1 SE+ (2.4 GHz receiver `373B:1085`) on Windows 11

Other Compx-based mice should work. Linux and macOS builds are tested in CI but not yet with a real mouse.
Please open an issue with the output of `atkctl info -v`.
