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

    /// <summary>Writes up to 10 bytes of EEPROM, then reads them back to verify.</summary>
    public void WriteEeprom(ushort address, ReadOnlySpan<byte> data)
    {
        if (data.Length is 0 or > CompxPacket.MaxData)
            throw new ArgumentException($"1..{CompxPacket.MaxData} bytes expected", nameof(data));

        var req = new CompxPacket(CompxCommand.SetEeprom, address, (byte)data.Length);
        req.SetData(data);
        var resp = Transport.Transfer(req);
        if (resp.Status != 0)
            throw new InvalidOperationException($"Write to {address} failed, status {resp.Status}");

        var check = ReadEeprom(address, (byte)data.Length);
        if (!check.AsSpan().SequenceEqual(data))
            throw new InvalidOperationException(
                $"Verify failed at {address}: wrote {Convert.ToHexString(data)}, read {Convert.ToHexString(check)}");
    }

    /// <summary>Sets the color of DPI slot 0..7. The neighbouring slot in the same chunk is kept.</summary>
    public void SetDpiColor(int slot, RgbColor color)
    {
        if (slot is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(slot));
        var address = (ushort)(EepromAddress.DpiColor1 + 8 * (slot / 2));
        var chunk = ReadEeprom(address, 8);
        int offset = 4 * (slot % 2);
        chunk[offset] = color.R;
        chunk[offset + 1] = color.G;
        chunk[offset + 2] = color.B;
        chunk[offset + 3] = (byte)(0x55 - (color.R + color.G + color.B));
        WriteEeprom(address, chunk);
    }

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
