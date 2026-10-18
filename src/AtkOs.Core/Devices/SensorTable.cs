namespace AtkOs.Core.Devices;

/// <summary>Mouse model by CID/MID (reported by the mouse). Title is an i18n key or a name.</summary>
public static class SensorTable
{
    public sealed record Entry(string Title, string Ic);

    public static readonly Dictionary<(int Cid, int Mid), Entry> ByCidMid = new()
    {
        [(1, 1)] = new("VXE.R1 Pro", "52833"),
        [(2, 1)] = new("deviceName.fMouse.f1Pro", "52833"),
        [(2, 2)] = new("deviceName.fMouse.f1Pro", "52833"),
        [(2, 3)] = new("deviceName.fMouse.f1ProMax", "52840"),
        [(2, 4)] = new("deviceName.fMouse.f1ProMax", "52840"),
        [(2, 5)] = new("deviceName.fMouse.f1Moba", "52840-moba"),
        [(2, 6)] = new("deviceName.fMouse.f1Moba", "52840-moba"),
        [(2, 7)] = new("deviceName.fMouse.r1pro", "r1-pro"),
        [(2, 8)] = new("deviceName.fMouse.r1pro", "r1-pro"),
        [(2, 9)] = new("deviceName.fMouse.r1Ultra", "r1-ultra"),
        [(2, 11)] = new("deviceName.fMouse.r1", "r1"),
        [(2, 12)] = new("deviceName.fMouse.r1", "r1"),
        [(2, 19)] = new("deviceName.fMouse.f1s", "CX52850P"),
        [(2, 20)] = new("deviceName.fMouse.f1s", "CX52850P"),
        [(2, 27)] = new("deviceName.fMouse.r1promax", "r1-pro-max"),
        [(2, 28)] = new("deviceName.fMouse.r1promax", "r1-pro-max"),
        [(2, 29)] = new("deviceName.fMouse.r1se", "r1-se"),
        [(2, 30)] = new("deviceName.fMouse.r1se", "r1-se"),
        [(2, 31)] = new("deviceName.fMouse.r1se+", "r1-se+"),
        [(2, 32)] = new("deviceName.fMouse.r1se+", "r1-se+"),
        [(2, 33)] = new("deviceName.fMouse.f1Moba", "52840-moba"),
        [(2, 34)] = new("deviceName.fMouse.r1promax", "r1-pro-max"),
        [(2, 36)] = new("deviceName.fMouse.Blazing Sky F1 Pro", "sky-pro"),
        [(2, 37)] = new("deviceName.fMouse.Blazing Sky F1 Pro Max", "sky-pro-max"),
        [(2, 38)] = new("deviceName.fMouse.Blazing Sky X1 Pro", "sky-pro"),
        [(2, 39)] = new("deviceName.fMouse.Blazing Sky X1 Pro Max", "sky-pro-max"),
        [(2, 40)] = new("deviceName.fMouse.Blazing Sky Z1 Pro", "sky-z1-pro-max"),
        [(2, 41)] = new("deviceName.fMouse.Blazing Sky Z1 Pro Max", "sky-z1-pro-max"),
        [(2, 42)] = new("deviceName.fMouse.Blazing Sky F1 Ultimate", "sky-f1-ultimate"),
        [(2, 43)] = new("deviceName.fMouse.Blazing Sky X1 Ultra", "sky-x1-ultra"),
        [(2, 44)] = new("deviceName.fMouse.Fierce X", "fierce-x"),
        [(2, 45)] = new("deviceName.fMouse.Blazing Sky F1 Ultra", "sky-f1-ultra"),
        [(2, 46)] = new("deviceName.fMouse.Blazing Sky X1 Ultimate", "sky-x1-ultimate"),
        [(2, 47)] = new("deviceName.fMouse.K1 Major", "mad-r-major"),
        [(2, 48)] = new("deviceName.fMouse.f1ProMaxElden", "52840"),
        [(2, 49)] = new("deviceName.fMouse.K1", "mad-r"),
        [(2, 50)] = new("deviceName.fMouse.Blazing Sky F1 Extreme", "sky-f1-extreme"),
        [(2, 52)] = new("deviceName.fMouse.f1ProMaxJoJo", "52840"),
        [(2, 53)] = new("deviceName.fMouse.Blazing Sky Z1 Ultra", "sky-z1-ultra"),
        [(2, 54)] = new("deviceName.fMouse.MAD R +", "mad-r"),
        [(2, 55)] = new("deviceName.fMouse.MAD R Major+", "mad-r-major"),
        [(2, 56)] = new("deviceName.fMouse.Blazing Sky F1 Extreme Lev", "sky-f1-extreme"),
        [(2, 57)] = new("deviceName.fMouse.R1 Nearlink", "star-r1"),
        [(2, 58)] = new("deviceName.fMouse.Blazing Sky S1 Pro Max", "sky-s1-pro-max"),
        [(2, 59)] = new("deviceName.fMouse.Blazing Sky S1 Extreme", "sky-s1-extreme"),
        [(2, 61)] = new("deviceName.fMouse.Blazing Sky V1 Ultimate", "sky-v1-ultimate"),
        [(2, 62)] = new("deviceName.fMouse.Blazing Sky V1 Pro Max", "sky-v1-pro-max"),
        [(2, 63)] = new("deviceName.fMouse.R1 S+", "r1-s+"),
        [(2, 64)] = new("deviceName.fMouse.Blazing Sky U2 Mini", "3950"),
        [(2, 65)] = new("deviceName.fMouse.Blazing Sky U2 Ultra", "3950Ultra"),
        [(2, 66)] = new("deviceName.fMouse.Blazing Sky U2 Pro Max", "3950"),
        [(2, 67)] = new("deviceName.fMouse.R1 Nearlink", "star-r1"),
        [(2, 70)] = new("deviceName.fMouse.Blazing Sky X1 Extreme", "sky-x1-ultimate"),
        [(0, 0)] = new("Device not connected", ""),
    };

    public static Entry Lookup(int cid, int mid) => ByCidMid.TryGetValue((cid, mid), out var e) ? e : new("Unknown", "");
}
