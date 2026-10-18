using AtkOs.Core.Protocol;

namespace AtkOs.Core.Hid;

/// <summary>Sends one command packet and returns the matching reply.</summary>
public interface ICompxTransport : IDisposable
{
    CompxPacket Transfer(CompxPacket request, int timeoutMs = 1000);
}
