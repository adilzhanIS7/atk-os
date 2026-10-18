using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;

namespace AtkOs.Core.Tests;

/// <summary>
/// Emulates a Compx mouse: a 512-byte EEPROM plus the identity/battery commands.
/// Records every packet sent, so tests can compare them with captures from real hardware.
/// </summary>
public sealed class FakeMouse : ICompxTransport
{
    public byte[] Eeprom { get; } = new byte[512];
    public List<string> Sent { get; } = [];

    public bool Online { get; set; } = true;
    public byte Cid { get; set; } = 2;
    public byte Mid { get; set; } = 31;
    public ConnectionType Connection { get; set; } = ConnectionType.Dongle1K;

    /// <summary>Simulates a mouse that acknowledges writes but does not store them.</summary>
    public bool IgnoreWrites { get; set; }

    /// <summary>Non-zero makes every reply report this error status.</summary>
    public byte FailStatus { get; set; }

    public CompxPacket Transfer(CompxPacket request, int timeoutMs = 1000)
    {
        var bytes = request.ToBytes();
        Sent.Add("08" + Convert.ToHexString(bytes));

        var reply = new CompxPacket(request.Command, request.Address, request.DataLength);
        reply.Raw[1] = FailStatus;

        switch (request.Command)
        {
            case CompxCommand.GetWirelessMouseOnline:
                reply[0] = Online ? (byte)1 : (byte)0;
                break;
            case CompxCommand.Handshake:
                reply[4] = Cid;
                reply[5] = Mid;
                reply[6] = (byte)Connection;
                break;
            case CompxCommand.GetBatteryLevel:
                reply[0] = 95;
                reply[2] = 0x0F;
                reply[3] = 0xDE;
                break;
            case CompxCommand.GetMouseVersion:
                reply[0] = 0x03;
                break;
            case CompxCommand.GetEeprom:
                reply.SetData(Eeprom.AsSpan(request.Address, request.DataLength));
                break;
            case CompxCommand.SetEeprom:
                if (!IgnoreWrites && FailStatus == 0)
                    request.Data[..request.DataLength].CopyTo(Eeprom.AsSpan(request.Address));
                reply.SetData(request.Data);
                break;
        }
        return new CompxPacket(reply.ToBytes());
    }

    public void Dispose() { }
}
