using AtkOs.Core.Protocol;
using HidSharp;

namespace AtkOs.Core.Hid;

/// <summary>
/// Talks to the vendor-defined HID collection of a Compx mouse or receiver:
/// sends a packet, waits for the reply with the same command ID.
/// </summary>
public sealed class CompxTransport : IDisposable
{
    private readonly HidStream _stream;
    private readonly byte[] _readBuffer;
    private readonly object _lock = new();

    public HidDevice Device { get; }

    /// <summary>Raw traffic log, e.g. for the --verbose flag.</summary>
    public Action<string>? Log { get; set; }

    /// <summary>Unsolicited "status changed" packets (command 10) received while waiting.</summary>
    public event Action<CompxPacket>? StatusChanged;

    public CompxTransport(HidDevice device)
    {
        Device = device;
        _stream = device.Open();
        _stream.ReadTimeout = 1000;
        _stream.WriteTimeout = 1000;
        _readBuffer = new byte[Math.Max(device.GetMaxInputReportLength(), CompxPacket.Size + 1)];
    }

    /// <summary>
    /// The collection that carries the command channel: it has output report ID 8.
    /// (WebHID selects the interface by usage 0xFF04:0x02, but on Windows each top-level
    /// collection is a separate device, and report 8 lives in a sibling collection, usage 0xFF02.)
    /// </summary>
    public static bool IsVendorInterface(HidDevice d)
    {
        try
        {
            return d.GetMaxOutputReportLength() >= CompxPacket.Size + 1
                && d.GetReportDescriptor().OutputReports.Any(r => r.ReportID == CompxPacket.ReportId);
        }
        catch
        {
            return false; // descriptor not readable
        }
    }

    public CompxPacket Transfer(CompxPacket request, int timeoutMs = 1000)
    {
        lock (_lock)
        {
            var payload = request.ToBytes();
            var report = new byte[Math.Max(Device.GetMaxOutputReportLength(), CompxPacket.Size + 1)];
            report[0] = CompxPacket.ReportId;
            payload.CopyTo(report, 1);

            Log?.Invoke($"TX {Convert.ToHexString(report)}");
            _stream.Write(report);

            var deadline = Environment.TickCount64 + timeoutMs;
            while (Environment.TickCount64 < deadline)
            {
                int n;
                try
                {
                    n = _stream.Read(_readBuffer);
                }
                catch (TimeoutException)
                {
                    break;
                }
                // On Linux/macOS one handle receives every report of the interface
                // (mouse movement etc.), so keep only the command channel.
                if (n < CompxPacket.Size + 1 || _readBuffer[0] != CompxPacket.ReportId) continue;

                Log?.Invoke($"RX {Convert.ToHexString(_readBuffer, 0, n)}");
                var response = new CompxPacket(_readBuffer.AsSpan(1, CompxPacket.Size).ToArray());

                if (response.Command == CompxCommand.StatusChanged && request.Command != CompxCommand.StatusChanged)
                {
                    StatusChanged?.Invoke(response);
                    continue;
                }
                if (response.Command != request.Command) continue;
                if (IsEeprom(request.Command) && response.Address != request.Address) continue;
                return response;
            }
            throw new TimeoutException($"No reply to {request.Command} (addr {request.Address})");
        }
    }

    private static bool IsEeprom(CompxCommand c) => c is CompxCommand.GetEeprom or CompxCommand.SetEeprom;

    public void Dispose() => _stream.Dispose();
}
