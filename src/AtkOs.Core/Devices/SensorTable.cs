namespace AtkOs.Core.Devices;

/// <summary>Mouse model by CID/MID (reported by the mouse in the handshake).</summary>
public static class SensorTable
{
    public sealed record Entry(string Title, string Ic);

    public static readonly Dictionary<(int Cid, int Mid), Entry> ByCidMid = new()
    {
        [(1, 1)] = new("VXE.R1 Pro", "52833"),
        [(2, 1)] = new("Dragonfly F1 Pro", "52833"),
        [(2, 2)] = new("Dragonfly F1 Pro", "52833"),
        [(2, 3)] = new("Dragonfly F1 Pro Max", "52840"),
        [(2, 4)] = new("Dragonfly F1 Pro Max", "52840"),
        [(2, 5)] = new("Dragonfly F1 MOBA", "52840-moba"),
        [(2, 6)] = new("Dragonfly F1 MOBA", "52840-moba"),
        [(2, 7)] = new("Dragonfly R1 Pro", "r1-pro"),
        [(2, 8)] = new("Dragonfly R1 Pro", "r1-pro"),
        [(2, 9)] = new("Dragonfly R1 Ultra", "r1-ultra"),
        [(2, 11)] = new("Dragonfly R1", "r1"),
        [(2, 12)] = new("Dragonfly R1", "r1"),
        [(2, 19)] = new("Dragonfly F1 S", "CX52850P"),
        [(2, 20)] = new("Dragonfly F1 S", "CX52850P"),
        [(2, 27)] = new("Dragonfly R1 Pro Max", "r1-pro-max"),
        [(2, 28)] = new("Dragonfly R1 Pro Max", "r1-pro-max"),
        [(2, 29)] = new("Dragonfly R1 SE", "r1-se"),
        [(2, 30)] = new("Dragonfly R1 SE", "r1-se"),
        [(2, 31)] = new("Dragonfly R1 SE+", "r1-se+"),
        [(2, 32)] = new("Dragonfly R1 SE+", "r1-se+"),
        [(2, 33)] = new("Dragonfly F1 MOBA", "52840-moba"),
        [(2, 34)] = new("Dragonfly R1 Pro Max", "r1-pro-max"),
        [(2, 36)] = new("Blazing Sky F1 Pro", "sky-pro"),
        [(2, 37)] = new("Blazing Sky F1 Pro Max", "sky-pro-max"),
        [(2, 38)] = new("Blazing Sky X1 Pro", "sky-pro"),
        [(2, 39)] = new("Blazing Sky X1 Pro Max", "sky-pro-max"),
        [(2, 40)] = new("Blazing Sky Z1 Pro", "sky-z1-pro-max"),
        [(2, 41)] = new("Blazing Sky Z1 Pro Max", "sky-z1-pro-max"),
        [(2, 42)] = new("Blazing Sky F1 Ultimate", "sky-f1-ultimate"),
        [(2, 43)] = new("Blazing Sky X1 Ultra", "sky-x1-ultra"),
        [(2, 44)] = new("Fierce X", "fierce-x"),
        [(2, 45)] = new("Blazing Sky F1 Ultra", "sky-f1-ultra"),
        [(2, 46)] = new("Blazing Sky X1 Ultimate", "sky-x1-ultimate"),
        [(2, 47)] = new("K1 Major", "mad-r-major"),
        [(2, 48)] = new("Dragonfly F1 Pro Max Elden Ring Collaboration Edition", "52840"),
        [(2, 49)] = new("VXE K1", "mad-r"),
        [(2, 50)] = new("Blazing Sky F1 Extreme", "sky-f1-extreme"),
        [(2, 52)] = new("f1ProMaxJoJo", "52840"),
        [(2, 53)] = new("Blazing Sky Z1 Ultra", "sky-z1-ultra"),
        [(2, 54)] = new("MAD R +", "mad-r"),
        [(2, 55)] = new("MAD R Major+", "mad-r-major"),
        [(2, 56)] = new("Blazing Sky F1 Extreme Lev", "sky-f1-extreme"),
        [(2, 57)] = new("R1 Nearlink", "star-r1"),
        [(2, 58)] = new("Blazing Sky S1 Pro Max", "sky-s1-pro-max"),
        [(2, 59)] = new("Blazing Sky S1 Extreme", "sky-s1-extreme"),
        [(2, 61)] = new("Blazing Sky V1 Ultimate", "sky-v1-ultimate"),
        [(2, 62)] = new("Blazing Sky V1 Pro Max", "sky-v1-pro-max"),
        [(2, 63)] = new("R1 S+", "r1-s+"),
        [(2, 64)] = new("Blazing Sky U2 Mini", "3950"),
        [(2, 65)] = new("Blazing Sky U2 Ultra", "3950Ultra"),
        [(2, 66)] = new("Blazing Sky U2 Pro Max", "3950"),
        [(2, 67)] = new("R1 Nearlink", "star-r1"),
        [(2, 70)] = new("Blazing Sky X1 Extreme", "sky-x1-ultimate"),
        [(0, 0)] = new("Device not connected", ""),
    };

    public static Entry Lookup(int cid, int mid) =>
        ByCidMid.TryGetValue((cid, mid), out var e) ? e : new($"Unknown mouse (CID {cid}, MID {mid})", "");
}
