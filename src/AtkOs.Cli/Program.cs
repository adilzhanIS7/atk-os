using AtkOs.Core;
using AtkOs.Core.Devices;
using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;

// atkctl — read-only diagnostic tool for ATK (Compx) mice.
//   atkctl list              list detected devices
//   atkctl info [-v]         battery, firmware, DPI, polling rate, ...
//   atkctl dump [file] [-v]  back up EEPROM (addresses 0..511) to a file

var verbose = args.Contains("-v") || args.Contains("--verbose");
var positional = args.Where(a => !a.StartsWith('-')).ToArray();
var command = positional.FirstOrDefault() ?? "info";

try
{
    return command switch
    {
        "list" => List(),
        "info" => Info(),
        "dump" => Dump(positional.ElementAtOrDefault(1) ?? $"eeprom-{DateTime.Now:yyyyMMdd-HHmmss}.bin"),
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
    Console.WriteLine("usage: atkctl [list | info | dump [file]] [-v]");
    return 2;
}

int List()
{
    var known = KnownDevices.Find().ToList();
    foreach (var d in known) Console.WriteLine(d);

    var unknown = KnownDevices.FindAnyCompx()
        .Where(h => known.All(k => k.Hid.DevicePath != h.DevicePath)).ToList();
    foreach (var h in unknown)
        Console.WriteLine($"{h.VendorID:X4}:{h.ProductID:X4} unknown Compx-like device  {h.DevicePath}");

    if (known.Count == 0 && unknown.Count == 0) Console.WriteLine("No supported devices found.");
    return 0;
}

CompxMouse Open()
{
    var dev = KnownDevices.Find().FirstOrDefault()
        ?? throw new InvalidOperationException("No supported mouse found. Run `atkctl list`.");
    Console.WriteLine($"Device:   {dev}");
    var transport = new CompxTransport(dev.Hid);
    if (verbose) transport.Log = Console.WriteLine;
    var mouse = new CompxMouse(transport);

    if (dev.Model.IsReceiver && !mouse.IsOnline())
        throw new InvalidOperationException("Receiver found, but the mouse is off or asleep. Move it and retry.");
    return mouse;
}

int Info()
{
    var mouse = Open();
    using var _ = mouse.Transport;

    var (cid, mid, type) = mouse.Handshake();
    var model = SensorTable.Lookup(cid, mid);
    var encoding = KnownDevices.DpiEncodingFor(model.Ic);
    Console.WriteLine($"Model:    {model.Title} (CID {cid}, MID {mid}, IC {model.Ic}, type {type}, DPI {encoding})");
    Console.WriteLine($"Firmware: {mouse.GetFirmwareVersion()}");

    var bat = mouse.GetBattery();
    Console.WriteLine($"Battery:  {bat.Percent}%{(bat.Charging ? " (charging)" : "")}, {bat.Millivolts} mV");
    Console.WriteLine($"Profile:  {mouse.GetCurrentProfile() + 1}");

    var rr = mouse.GetReportRateDpi();
    Console.WriteLine($"Polling:  {rr.ReportRate.ToHz()} Hz");
    Console.WriteLine($"LOD:      {mouse.GetLiftOffDistance()}");

    var slots = mouse.GetDpiSlots();
    var colors = mouse.GetDpiColors();
    Console.WriteLine($"DPI:      {rr.DpiSlotCount} slots, active #{rr.CurrentDpiSlot + 1}");
    for (int i = 0; i < Math.Min(rr.DpiSlotCount, slots.Count); i++)
    {
        var (x, y) = DpiCodec.Decode(slots[i], encoding);
        var mark = i == rr.CurrentDpiSlot ? "*" : " ";
        var dpi = x == y ? $"{x}" : $"{x}x{y}";
        Console.WriteLine($"  {mark}{i + 1}: {dpi,-7} color {colors[i]}  raw {slots[i]}");
    }

    var p = mouse.GetPerformance();
    Console.WriteLine($"Debounce: {p.Debounce}   MotionSync: {p.MotionSync}   AngleSnap: {p.AngleSnapping}   Ripple: {p.RippleControl}");
    Console.WriteLine($"Sleep:    enabled {p.SensorSleepEnabled}, time {p.SensorSleepTime}   SensorMode: {p.SensorMode}   LED off: {p.LedOffTime}");
    return 0;
}

int Dump(string path)
{
    var mouse = Open();
    using var _ = mouse.Transport;
    mouse.Handshake();

    const int size = 512;
    var data = new byte[size];
    for (int addr = 0; addr < size; addr += CompxPacket.MaxData)
    {
        var len = (byte)Math.Min(CompxPacket.MaxData, size - addr);
        mouse.ReadEeprom((ushort)addr, len).CopyTo(data, addr);
        Console.Write($"\rReading {addr + len}/{size}");
    }
    File.WriteAllBytes(path, data);
    Console.WriteLine($"\nSaved: {Path.GetFullPath(path)}");
    return 0;
}
