namespace AtkOs.Core.Protocol;

/// <summary>Connection type reported by the handshake (byte 6).</summary>
public enum ConnectionType : byte
{
    Dongle1K = 0,
    Dongle4K = 1,
    Wired1K = 2,
    Wired8K = 3,
    Dongle2K = 4,
    Dongle8K = 5,
}

/// <summary>Allowed values, as offered by the official app.</summary>
public static class Options
{
    public static readonly int[] DebounceMs = [0, 1, 2, 4, 8, 15, 20];

    /// <summary>LOD: raw value → label. 0.7 mm exists only on some sensors.</summary>
    public static readonly (byte Raw, string Label)[] LiftOff = [(1, "1 mm"), (2, "2 mm")];

    public static ReportRate[] ReportRatesFor(ConnectionType type)
    {
        var list = new List<ReportRate>();
        if (type is ConnectionType.Dongle8K or ConnectionType.Wired8K)
            list.AddRange([ReportRate.Hz8000, ReportRate.Hz4000, ReportRate.Hz2000]);
        if (type == ConnectionType.Dongle4K) list.AddRange([ReportRate.Hz4000, ReportRate.Hz2000]);
        if (type == ConnectionType.Dongle2K) list.Add(ReportRate.Hz2000);
        list.AddRange([ReportRate.Hz1000, ReportRate.Hz500, ReportRate.Hz250, ReportRate.Hz125]);
        return [.. list];
    }

    public static ReportRate FromHz(int hz) => hz switch
    {
        125 => ReportRate.Hz125,
        250 => ReportRate.Hz250,
        500 => ReportRate.Hz500,
        1000 => ReportRate.Hz1000,
        2000 => ReportRate.Hz2000,
        4000 => ReportRate.Hz4000,
        8000 => ReportRate.Hz8000,
        _ => throw new ArgumentOutOfRangeException(nameof(hz), $"{hz} Hz is not a valid polling rate"),
    };
}
