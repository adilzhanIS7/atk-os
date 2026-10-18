using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;

namespace AtkOs.Core;

public sealed record BatteryInfo(int Percent, bool Charging, int Millivolts);

public sealed record ReportRateDpi(ReportRate ReportRate, int DpiSlotCount, int CurrentDpiSlot);

public sealed record PerformanceSettings(
    int Debounce, int MotionSync, int LedOffTime, int AngleSnapping, int RippleControl,
    int MoveCloseLed, int SensorSleepEnabled, int SensorSleepTime, int SensorMode, int RfTxTime);

public sealed record RgbColor(byte R, byte G, byte B)
{
    public override string ToString() => $"#{R:X2}{G:X2}{B:X2}";
}

/// <summary>High-level API for Compx mice. Read-only for now.</summary>
public sealed class CompxMouse(CompxTransport transport)
{
    public CompxTransport Transport { get; } = transport;

    private CompxPacket Send(CompxCommand cmd, ushort address = 0, byte len = 0)
    {
        var resp = Transport.Transfer(new CompxPacket(cmd, address, len));
        if (resp.Status != 0)
            throw new InvalidOperationException($"{cmd} failed, status {resp.Status}");
        return resp;
    }

    /// <summary>Reads up to 10 bytes of EEPROM.</summary>
    public byte[] ReadEeprom(ushort address, byte length = CompxPacket.MaxData) =>
        Send(CompxCommand.GetEeprom, address, length).Data[..length].ToArray();

    /// <summary>For a receiver: is the mouse switched on and paired?</summary>
    public bool IsOnline() => Send(CompxCommand.GetWirelessMouseOnline)[0] == 1;

    /// <summary>Same handshake the official app sends on connect. Returns CID, MID, device type.</summary>
    public (byte Cid, byte Mid, byte DeviceType) Handshake()
    {
        var req = new CompxPacket(CompxCommand.Handshake, 0, 8);
        req.SetData(BitConverter.GetBytes(Random.Shared.Next()));
        var r = Transport.Transfer(req);
        return (r[4], r[5], r[6]);
    }

    public BatteryInfo GetBattery()
    {
        var r = Send(CompxCommand.GetBatteryLevel);
        return new BatteryInfo(r[0], r[1] != 0, r[2] << 8 | r[3]);
    }

    public string GetFirmwareVersion()
    {
        var r = Send(CompxCommand.GetMouseVersion);
        return $"{r[0]:x2}{r[1]:x2}";
    }

    public (byte Cid, byte Mid) GetCidMid()
    {
        var r = Send(CompxCommand.GetMouseCidMid);
        return (r[0], r[1]);
    }

    public int GetCurrentProfile() => Send(CompxCommand.GetCurrentProfile)[0];

    public ReportRateDpi GetReportRateDpi()
    {
        var d = ReadEeprom(EepromAddress.ReportRate, 6);
        return new ReportRateDpi((ReportRate)d[0], d[2], d[4]);
    }

    public int GetLiftOffDistance() => ReadEeprom(EepromAddress.SilentHeight, 2)[0];

    /// <summary>All 8 DPI slots (only the first DpiSlotCount are in use).</summary>
    public List<DpiSlot> GetDpiSlots()
    {
        var list = new List<DpiSlot>();
        for (int i = 0; i < 4; i++)
        {
            var d = ReadEeprom((ushort)(EepromAddress.Dpi1 + 8 * i), 8);
            list.Add(new DpiSlot(d[0], d[1], d[2]));
            list.Add(new DpiSlot(d[4], d[5], d[6]));
        }
        return list;
    }

    public List<RgbColor> GetDpiColors()
    {
        var list = new List<RgbColor>();
        for (int i = 0; i < 4; i++)
        {
            var d = ReadEeprom((ushort)(EepromAddress.DpiColor1 + 8 * i), 8);
            list.Add(new RgbColor(d[0], d[1], d[2]));
            list.Add(new RgbColor(d[4], d[5], d[6]));
        }
        return list;
    }

    public PerformanceSettings GetPerformance()
    {
        var a = ReadEeprom(EepromAddress.StabilizationTime, 10);
        var b = ReadEeprom(EepromAddress.MoveCloseLights, 10);
        return new PerformanceSettings(a[0], a[2], a[4], a[6], a[8], b[0], b[2], b[4], b[6], b[8]);
    }
}
