using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;
using HidSharp;

namespace AtkOs.Core.Devices;

public sealed record DeviceModel(int VendorId, int ProductId, string Family, bool IsReceiver);

public sealed record FoundDevice(DeviceModel Model, HidDevice Hid)
{
    public override string ToString() =>
        $"{Model.VendorId:X4}:{Model.ProductId:X4} {Model.Family}{(Model.IsReceiver ? " (receiver)" : "")}  {Hid.DevicePath}";
}

public static class KnownDevices
{
    // Compx mice. Only models confirmed on real hardware are listed here;
    // the full table from ATK V HUB is added as people test their mice.
    public static readonly DeviceModel[] Mice =
    [
        new(0x373B, 0x1084, "compx-3311", false),
        new(0x373B, 0x1085, "compx-3311", true),
        new(0x3554, 0xFB12, "compx-3311", true),
    ];

    /// <summary>Sensor ICs (looked up by CID/MID) that use 10-DPI steps.</summary>
    private static readonly HashSet<string> Step10Ics =
    [
        "sky-f1-ultimate", "sky-f1-extreme", "sky-f1-ultra", "sky-v1-ultimate", "sky-x1-ultra",
        "sky-x1-ultimate", "sky-s1-extreme", "sky-s1-pro-max", "sky-z1-ultra", "fierce-x", "3950Ultra",
    ];

    /// <summary>Max DPI per sensor IC (models not listed: 26000).</summary>
    public static readonly Dictionary<string, int> MaxDpi = new()
    {
        ["r1-s+"] = 12000, ["r1-se"] = 18000, ["r1-se+"] = 18000, ["star-r1"] = 18000,
        ["fierce-x"] = 42000, ["mad-r-major"] = 36000, ["sky-pro"] = 36000, ["sky-pro-max"] = 36000,
        ["sky-f1-ultra"] = 42000, ["sky-f1-ultimate"] = 42000, ["sky-f1-extreme"] = 42000,
        ["sky-v1-pro-max"] = 36000, ["sky-v1-ultimate"] = 42000, ["sky-x1-ultra"] = 42000,
        ["sky-x1-ultimate"] = 42000, ["sky-z1-pro-max"] = 36000, ["sky-z1-ultra"] = 42000,
        ["sky-s1-pro-max"] = 42000, ["sky-s1-extreme"] = 42000, ["3950Ultra"] = 42000, ["3950"] = 36000,
    };

    public static int MaxDpiFor(string? ic) => ic != null && MaxDpi.TryGetValue(ic, out var v) ? v : 26000;

    public static DpiEncoding DpiEncodingFor(string? ic)
    {
        if (ic != null && Step10Ics.Contains(ic)) return DpiEncoding.Step10;
        return MaxDpiFor(ic) > 18000 ? DpiEncoding.Step50 : DpiEncoding.Table;
    }

    public static IEnumerable<FoundDevice> Find()
    {
        foreach (var d in DeviceList.Local.GetHidDevices())
        {
            var model = Mice.FirstOrDefault(m => m.VendorId == d.VendorID && m.ProductId == d.ProductID);
            if (model != null && CompxTransport.IsVendorInterface(d))
                yield return new FoundDevice(model, d);
        }
    }

    /// <summary>Every HID interface with the Compx vendor usage, known or not.</summary>
    public static IEnumerable<HidDevice> FindAnyCompx() =>
        DeviceList.Local.GetHidDevices().Where(CompxTransport.IsVendorInterface);
}
