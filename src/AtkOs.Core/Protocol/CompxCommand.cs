namespace AtkOs.Core.Protocol;

public enum CompxCommand : byte
{
    Handshake = 1,                 // "DownLoadData": 4 random bytes in, CID/MID/device type out
    DriverStatus = 2,
    GetWirelessMouseOnline = 3,
    GetBatteryLevel = 4,
    StartPairing = 5,
    GetPairingStatus = 6,
    SetEeprom = 7,
    GetEeprom = 8,
    RestoreFactory = 9,
    StatusChanged = 10,            // unsolicited event from the mouse
    EnterUsbUpgradeMode = 13,      // firmware update — never used by ATK-OS
    GetCurrentProfile = 14,
    SetCurrentProfile = 15,
    GetMouseCidMid = 16,
    GetMouseVersion = 18,
    ExitPairing = 19,
    Set4KRgbMode = 20,
    Get4KRgbMode = 21,
    SetFarDistanceMode = 22,
    GetFarDistanceMode = 23,
    SetDongleLightMode = 24,
    GetDongleLightMode = 25,
    UpgradeErrorStatus = 90,
    UpgradeStatus = 91,
}

/// <summary>EEPROM addresses. Most values are stored as a pair: value, then 0x55 - value.</summary>
public static class EepromAddress
{
    public const ushort ReportRate = 0;          // + MaxDpi(2), CurrentDpi(4)
    public const ushort SilentHeight = 10;       // lift-off distance
    public const ushort Dpi1 = 12;               // 8 DPI slots, 4 bytes each, read 2 per request
    public const ushort DpiColor1 = 44;          // 8 colors, 4 bytes each
    public const ushort DpiRgbEffects = 76;
    public const ushort ArticleLamp = 84;
    public const ushort Keys = 96;               // 16 buttons, 4 bytes each
    public const ushort StabilizationTime = 169; // debounce, motion sync, LED off, angle snap, ripple
    public const ushort MoveCloseLights = 179;   // sensor sleep, sensor mode, RF TX time
    public const ushort Shortcuts = 256;
}

public enum ReportRate : byte
{
    Hz1000 = 1,
    Hz500 = 2,
    Hz250 = 4,
    Hz125 = 8,
    Hz2000 = 16,
    Hz4000 = 32,
    Hz8000 = 64,
}

public static class ReportRateExtensions
{
    public static int ToHz(this ReportRate r) => r switch
    {
        ReportRate.Hz125 => 125,
        ReportRate.Hz250 => 250,
        ReportRate.Hz500 => 500,
        ReportRate.Hz1000 => 1000,
        ReportRate.Hz2000 => 2000,
        ReportRate.Hz4000 => 4000,
        ReportRate.Hz8000 => 8000,
        _ => 0,
    };
}
