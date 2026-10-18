using AtkOs.Core.Devices;
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

/// <summary>What the handshake tells us about the connected mouse.</summary>
public sealed record MouseIdentity(byte Cid, byte Mid, ConnectionType Connection, SensorTable.Entry Model)
{
    public DpiEncoding DpiEncoding => KnownDevices.DpiEncodingFor(Model.Ic);
    public int MaxDpi => KnownDevices.MaxDpiFor(Model.Ic);
}

/// <summary>Toggles stored in the "before performance" block (EEPROM 169..178).</summary>
public enum PerformanceToggle
{
    MotionSync = 1,
    AngleSnapping = 3,
    RippleControl = 4,
}

/// <summary>High-level API for Compx mice.</summary>
public sealed class CompxMouse(ICompxTransport transport)
{
    public ICompxTransport Transport { get; } = transport;

    private CompxPacket Send(CompxCommand cmd, ushort address = 0, byte len = 0)
    {
        var resp = Transport.Transfer(new CompxPacket(cmd, address, len));
        if (resp.Status != 0)
            throw new InvalidOperationException($"{cmd} failed, status {resp.Status}");
        return resp;
    }

    // ---------- EEPROM primitives ----------

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

    /// <summary>
    /// Changes one "value + (0x55 - value)" pair inside a block, keeping the rest of the block.
    /// </summary>
    private void WriteCheckedPair(ushort blockAddress, byte blockLength, int pairIndex, byte value)
    {
        var block = ReadEeprom(blockAddress, blockLength);
        block[2 * pairIndex] = value;
        block[2 * pairIndex + 1] = (byte)(0x55 - value);
        WriteEeprom(blockAddress, block);
    }

    /// <summary>Changes one 4-byte item (3 bytes + checksum) in an 8-byte chunk of two items.</summary>
    private void WriteQuad(ushort firstChunk, int index, byte a, byte b, byte c)
    {
        var address = (ushort)(firstChunk + 8 * (index / 2));
        var chunk = ReadEeprom(address, 8);
        int o = 4 * (index % 2);
        chunk[o] = a;
        chunk[o + 1] = b;
        chunk[o + 2] = c;
        chunk[o + 3] = (byte)(0x55 - (a + b + c));
        WriteEeprom(address, chunk);
    }

    // ---------- Identity & status ----------

    /// <summary>For a receiver: is the mouse switched on and paired?</summary>
    public bool IsOnline() => Send(CompxCommand.GetWirelessMouseOnline)[0] == 1;

    /// <summary>Same handshake the official app sends on connect.</summary>
    public MouseIdentity Handshake()
    {
        var req = new CompxPacket(CompxCommand.Handshake, 0, 8);
        req.SetData(BitConverter.GetBytes(Random.Shared.Next()));
        var r = Transport.Transfer(req);
        return new MouseIdentity(r[4], r[5], (ConnectionType)r[6], SensorTable.Lookup(r[4], r[5]));
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

    public int GetCurrentProfile() => Send(CompxCommand.GetCurrentProfile)[0];

    // ---------- Polling rate & DPI ----------

    public ReportRateDpi GetReportRateDpi()
    {
        var d = ReadEeprom(EepromAddress.ReportRate, 6);
        return new ReportRateDpi((ReportRate)d[0], d[2], d[4]);
    }

    public void SetReportRate(ReportRate rate) =>
        WriteCheckedPair(EepromAddress.ReportRate, 6, 0, (byte)rate);

    public void SetDpiSlotCount(int count)
    {
        if (count is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(count));
        WriteCheckedPair(EepromAddress.ReportRate, 6, 1, (byte)count);
    }

    public void SetCurrentDpiSlot(int slot)
    {
        if (slot is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(slot));
        WriteCheckedPair(EepromAddress.ReportRate, 6, 2, (byte)slot);
    }

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

    public void SetDpiSlot(int slot, DpiSlot value)
    {
        if (slot is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(slot));
        WriteQuad(EepromAddress.Dpi1, slot, value.XLow, value.YLow, value.Ext);
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

    /// <summary>Sets the color of DPI slot 0..7.</summary>
    public void SetDpiColor(int slot, RgbColor color)
    {
        if (slot is < 0 or > 7) throw new ArgumentOutOfRangeException(nameof(slot));
        WriteQuad(EepromAddress.DpiColor1, slot, color.R, color.G, color.B);
    }

    // ---------- Sensor & buttons ----------

    public int GetLiftOffDistance() => ReadEeprom(EepromAddress.SilentHeight, 2)[0];

    public void SetLiftOffDistance(byte raw)
    {
        if (raw is < 1 or > 3) throw new ArgumentOutOfRangeException(nameof(raw));
        WriteCheckedPair(EepromAddress.SilentHeight, 2, 0, raw);
    }

    public PerformanceSettings GetPerformance()
    {
        var a = ReadEeprom(EepromAddress.StabilizationTime, 10);
        var b = ReadEeprom(EepromAddress.MoveCloseLights, 10);
        return new PerformanceSettings(a[0], a[2], a[4], a[6], a[8], b[0], b[2], b[4], b[6], b[8]);
    }

    public void SetDebounce(int ms)
    {
        if (!Options.DebounceMs.Contains(ms))
            throw new ArgumentOutOfRangeException(nameof(ms), $"Allowed: {string.Join(", ", Options.DebounceMs)} ms");
        WriteCheckedPair(EepromAddress.StabilizationTime, 10, 0, (byte)ms);
    }

    public void SetToggle(PerformanceToggle toggle, bool on) =>
        WriteCheckedPair(EepromAddress.StabilizationTime, 10, (int)toggle, on ? (byte)1 : (byte)0);
}
