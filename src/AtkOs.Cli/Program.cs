using AtkOs.Core;
using AtkOs.Core.Devices;
using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;

// atkctl — command-line tool for ATK (Compx) mice.
//   atkctl list                     list detected devices
//   atkctl info [-v]                battery, firmware, DPI, polling rate, ...
//   atkctl dump [file] [-v]         back up EEPROM (addresses 0..511) to a file
//   atkctl set <setting> <value>    change a setting (see usage)

var verbose = args.Contains("-v") || args.Contains("--verbose");
var positional = args.Where(a => !a.StartsWith('-') || a.Length > 1 && char.IsDigit(a[1])).ToArray();
var command = positional.FirstOrDefault() ?? "info";

try
{
    return command switch
    {
        "list" => List(),
        "info" => Info(),
        "dump" => Dump(positional.ElementAtOrDefault(1) ?? $"eeprom-{DateTime.Now:yyyyMMdd-HHmmss}.bin"),
        "set" when positional.Length >= 3 => Set(positional[1], positional[2..]),
        _ => Usage(),
    };
}
catch (Exception e)
{
    Console.Error.WriteLine($"Error: {e.Message}");
    return 1;
}

int Usage()
{
    Console.WriteLine("""
        usage: atkctl <command> [-v]

          list                       list detected devices
          info                       show mouse status and settings
          dump [file]                back up EEPROM to a file

          set rate <hz>              polling rate: 125 250 500 1000 (2000 4000 8000 on 2K/4K/8K receivers)
          set dpi <slot> <dpi>       DPI value of slot 1-8
          set slot <n>               active DPI slot
          set slots <count>          number of DPI slots in the cycle (1-8)
          set color <slot> <RRGGBB>  DPI indicator color
          set lod <1|2>              lift-off distance, mm
          set debounce <ms>          0 1 2 4 8 15 20
          set motionsync <on|off>
          set anglesnap <on|off>
          set ripple <on|off>
        """);
    return 2;
}

int List()
{
    var found = KnownDevices.Find().ToList();
    foreach (var d in found) Console.WriteLine(d);
    if (found.Count == 0) Console.WriteLine("No supported devices found.");
    return 0;
}

(CompxMouse Mouse, MouseIdentity Id) Open()
{
    var dev = KnownDevices.Find().FirstOrDefault()
        ?? throw new InvalidOperationException("No supported mouse found. Run `atkctl list`.");
    if (verbose) Console.WriteLine($"Device:   {dev}");
    var transport = new CompxTransport(dev.Hid);
    if (verbose) transport.Log = Console.WriteLine;
    var mouse = new CompxMouse(transport);

    if (dev.Model.IsReceiver && !mouse.IsOnline())
    {
        transport.Dispose();
        throw new InvalidOperationException("Receiver found, but the mouse is off or asleep. Move it and retry.");
    }
    return (mouse, mouse.Handshake());
}

int Info()
{
    var (mouse, id) = Open();
    using var _ = mouse.Transport;

    Console.WriteLine($"Model:    {id.Model.Title} (CID {id.Cid}, MID {id.Mid}, IC {id.Model.Ic})");
    Console.WriteLine($"Link:     {id.Connection}, firmware {mouse.GetFirmwareVersion()}");

    var bat = mouse.GetBattery();
    Console.WriteLine($"Battery:  {bat.Percent}%{(bat.Charging ? " (charging)" : "")}, {bat.Millivolts} mV");
    Console.WriteLine($"Profile:  {mouse.GetCurrentProfile() + 1}");

    var rr = mouse.GetReportRateDpi();
    Console.WriteLine($"Polling:  {rr.ReportRate.ToHz()} Hz");
    Console.WriteLine($"LOD:      {mouse.GetLiftOffDistance()}");

    var slots = mouse.GetDpiSlots();
    var colors = mouse.GetDpiColors();
    Console.WriteLine($"DPI:      {rr.DpiSlotCount} slots, active #{rr.CurrentDpiSlot + 1}, max {id.MaxDpi}");
    for (int i = 0; i < Math.Min(rr.DpiSlotCount, slots.Count); i++)
    {
        var (x, y) = DpiCodec.Decode(slots[i], id.DpiEncoding);
        var mark = i == rr.CurrentDpiSlot ? "*" : " ";
        var dpi = x == y ? $"{x}" : $"{x}x{y}";
        Console.WriteLine($"  {mark}{i + 1}: {dpi,-7} color {colors[i]}  raw {slots[i]}");
    }

    var p = mouse.GetPerformance();
    Console.WriteLine($"Debounce: {p.Debounce} ms   MotionSync: {p.MotionSync}   AngleSnap: {p.AngleSnapping}   Ripple: {p.RippleControl}");
    return 0;
}

int Dump(string path)
{
    var (mouse, _) = Open();
    using var __ = mouse.Transport;

    const int size = 512;
    var data = new byte[size];
    for (int addr = 0; addr < size; addr += CompxPacket.MaxData)
    {
        var len = (byte)Math.Min(CompxPacket.MaxData, size - addr);
        mouse.ReadEeprom((ushort)addr, len).CopyTo(data, addr);
    }
    File.WriteAllBytes(path, data);
    Console.WriteLine($"Saved: {Path.GetFullPath(path)}");
    return 0;
}

int Set(string setting, string[] values)
{
    var (mouse, id) = Open();
    using var _ = mouse.Transport;

    int Slot(string s) => int.Parse(s) - 1;
    bool OnOff(string s) => s switch
    {
        "on" or "1" or "true" => true,
        "off" or "0" or "false" => false,
        _ => throw new ArgumentException("Expected on/off"),
    };
    string Need2() => values.Length >= 2 ? values[1] : throw new ArgumentException($"set {setting} needs two values");

    switch (setting)
    {
        case "rate":
            var rate = Options.FromHz(int.Parse(values[0]));
            if (!Options.ReportRatesFor(id.Connection).Contains(rate))
                throw new ArgumentException($"{values[0]} Hz is not supported on {id.Connection}");
            mouse.SetReportRate(rate);
            break;
        case "dpi":
            var dpi = int.Parse(Need2());
            if (dpi < 50 || dpi > id.MaxDpi) throw new ArgumentException($"DPI must be 50..{id.MaxDpi}");
            mouse.SetDpiSlot(Slot(values[0]), DpiCodec.Encode(dpi, id.DpiEncoding));
            break;
        case "slot":
            mouse.SetCurrentDpiSlot(Slot(values[0]));
            break;
        case "slots":
            mouse.SetDpiSlotCount(int.Parse(values[0]));
            break;
        case "color":
            var rgb = Convert.FromHexString(Need2().TrimStart('#'));
            if (rgb.Length != 3) throw new ArgumentException("Color must be RRGGBB");
            mouse.SetDpiColor(Slot(values[0]), new RgbColor(rgb[0], rgb[1], rgb[2]));
            break;
        case "lod":
            mouse.SetLiftOffDistance(byte.Parse(values[0]));
            break;
        case "debounce":
            mouse.SetDebounce(int.Parse(values[0]));
            break;
        case "motionsync":
            mouse.SetToggle(PerformanceToggle.MotionSync, OnOff(values[0]));
            break;
        case "anglesnap":
            mouse.SetToggle(PerformanceToggle.AngleSnapping, OnOff(values[0]));
            break;
        case "ripple":
            mouse.SetToggle(PerformanceToggle.RippleControl, OnOff(values[0]));
            break;
        default:
            return Usage();
    }
    Console.WriteLine($"OK: {setting} = {string.Join(' ', values)} (verified)");
    return 0;
}
