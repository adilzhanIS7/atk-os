# ATK-OS

Lightweight, open-source configuration tool for ATK / VXE / VGN gaming mice —
an alternative to the official **ATK V HUB** app.

> Unofficial project. Not affiliated with ATK, VXE or VGN.

The official app is an Electron bundle of ~430 MB with analytics and a built-in web server.
ATK-OS is a small native app in the spirit of [G-Helper](https://github.com/seerge/g-helper):
it sits in the tray, shows the battery level on its icon, and talks to the mouse directly over HID.
No telemetry, no accounts, no network.

## Features

- Battery level in the tray icon (green / orange / red, blue while charging)
- DPI levels: values, number of levels, active level
- Polling rate (125 Hz – 8 kHz, depending on the receiver)
- Lift-off distance, button debounce
- Motion sync, angle snapping, ripple control
- Windows, Linux, macOS (Avalonia UI)
- `atkctl` command-line tool for scripting and diagnostics

Every change is read back from the mouse to make sure it was stored.
Firmware updates are intentionally **not** supported.

## Supported devices

| Mouse | Connection | VID:PID | Status |
|---|---|---|---|
| VGN Dragonfly R1 SE+ | 2.4 GHz receiver | `373B:1085` | ✅ tested |
| Compx-based mice (Dragonfly F1 / R1, Blazing Sky, MAD R, …) | receiver / wired | `373B:*`, `3554:*` | ❓ should work, needs testers |

If you have another model, run `atkctl info -v` and open an issue with the output.

## Usage

Run `atk-os` — the window opens and the app stays in the tray when closed.
Start with `atk-os --tray` to go straight to the tray (useful for autostart).

```
atkctl list                      list detected devices
atkctl info                      show mouse status and settings
atkctl dump [file]               back up the mouse EEPROM to a file
atkctl set rate 1000             polling rate, Hz
atkctl set dpi 1 1600            DPI of level 1
atkctl set slot 2                switch to DPI level 2
atkctl set lod 1                 lift-off distance, mm
atkctl set debounce 4            button debounce, ms
atkctl set motionsync on         also: anglesnap, ripple
```

### Linux

Install the udev rule so the app can access the mouse without root:

```
sudo cp packaging/linux/70-atk-os.rules /etc/udev/rules.d/
sudo udevadm control --reload-rules && sudo udevadm trigger
```

Then re-plug the receiver.

## Building

Requires the .NET 8 SDK.

```
dotnet build
dotnet run --project src/AtkOs.App
```

Project layout:

- `src/AtkOs.Core` — protocol and HID transport (no UI)
- `src/AtkOs.App` — Avalonia tray app
- `src/AtkOs.Cli` — `atkctl`
- `docs/PROTOCOL.md` — protocol notes

## License

MIT
