namespace AtkOs.Core.Protocol;

/// <summary>
/// One DPI slot as stored in EEPROM: xLow, yLow, ext, check.
/// ext packs the high bits: bits 2-3 = X high, bits 6-7 = Y high,
/// bit 1 = "high range" (ultimate encoding), bits 0 and 4 = doubled value.
/// </summary>
public readonly record struct DpiSlot(byte XLow, byte YLow, byte Ext)
{
    public int XRaw => XLow | ((Ext & 0x0C) >> 2) << 8;
    public int YRaw => YLow | ((Ext & 0xC0) >> 6) << 8;
    public bool Doubled => (Ext & 0x11) != 0;

    public override string ToString() => $"{XLow:X2} {YLow:X2} {Ext:X2}";
}

public enum DpiEncoding
{
    /// <summary>50-DPI steps: dpi = 50 * (raw + 1). Most models.</summary>
    Step50,
    /// <summary>10-DPI steps up to 10000, 50-DPI steps above. "Ultimate"-class sensors.</summary>
    Step10,
    /// <summary>Lookup table (<see cref="DpiTable"/>). Sensors with max DPI ≤ 18000.</summary>
    Table,
}

public static class DpiCodec
{
    private const byte TableDoubleFlags = 0x22;

    public static (int X, int Y) Decode(DpiSlot s, DpiEncoding enc)
    {
        if (enc == DpiEncoding.Table)
        {
            var entry = DpiTable.Entries.FirstOrDefault(e => e.Raw == s.XLow);
            if (entry.Dpi == 0) return (0, 0); // unknown raw value
            int v = (s.Ext & 0x20) != 0 ? entry.Dpi * 2 : entry.Dpi;
            return (v, v);
        }

        int x, y;
        if (enc == DpiEncoding.Step50)
        {
            x = 50 * (s.XRaw + 1);
            y = 50 * (s.YRaw + 1);
        }
        else
        {
            bool high = (s.Ext & 0x02) != 0;
            x = high ? 50 * s.XRaw + 10050 : 10 * (s.XRaw + 1);
            y = high ? 50 * s.YRaw + 10050 : 10 * (s.YRaw + 1);
        }
        if (s.Doubled) { x *= 2; y *= 2; }
        return (x, y);
    }

    /// <summary>The closest DPI value the sensor can actually store.</summary>
    public static int Nearest(int dpi, DpiEncoding enc, int maxDpi)
    {
        dpi = Math.Clamp(dpi, 50, maxDpi);
        if (enc == DpiEncoding.Table)
        {
            var values = DpiTable.Entries.Select(e => e.Dpi)
                .Concat(DpiTable.Entries.Select(e => e.Dpi * 2).Where(v => v > 10000))
                .Where(v => v <= maxDpi);
            return values.MinBy(v => Math.Abs(v - dpi));
        }
        int step = enc == DpiEncoding.Step10 && dpi <= 10000 ? 10 : dpi > 30000 ? 100 : 50;
        return Math.Clamp((int)Math.Round(dpi / (double)step) * step, step, maxDpi);
    }

    public static DpiSlot Encode(int dpi, DpiEncoding enc)
    {
        if (enc == DpiEncoding.Table)
        {
            bool doubled = dpi > 10000;
            if (doubled && dpi % 100 != 0) dpi += 50; // above 10000 only even-hundred steps exist
            int target = doubled ? dpi / 2 : dpi;
            var entry = DpiTable.Entries.FirstOrDefault(e => e.Dpi == target);
            if (entry.Dpi == 0) throw new ArgumentOutOfRangeException(nameof(dpi), $"{dpi} DPI is not supported by this sensor");
            return new DpiSlot(entry.Raw, entry.Raw, doubled ? TableDoubleFlags : (byte)0);
        }

        int raw;
        byte ext = 0;
        if (enc == DpiEncoding.Step50)
        {
            if (dpi > 30000) { raw = dpi / 2 / 50 - 1; ext |= 0x11; }
            else raw = dpi / 50 - 1;
        }
        else if (dpi <= 10000)
        {
            raw = dpi / 10 - 1;
        }
        else
        {
            raw = dpi < 30000 ? (dpi - 10050) / 50 : (dpi / 2 - 10050) / 50;
            ext |= 0x22;
            if (dpi >= 30000) ext |= 0x11;
        }
        int hi = (raw >> 8) & 3;
        ext = (byte)(ext | hi << 6 | hi << 2);
        if (enc == DpiEncoding.Step10 && dpi <= 10000) ext &= unchecked((byte)~0x22);
        return new DpiSlot((byte)raw, (byte)raw, ext);
    }
}
