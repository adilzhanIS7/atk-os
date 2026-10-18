using AtkOs.Core;
using AtkOs.Core.Devices;
using AtkOs.Core.Hid;
using AtkOs.Core.Protocol;

namespace AtkOs.App.Services;

/// <summary>Everything the UI shows, read in one go.</summary>
public sealed record MouseSnapshot(
    MouseIdentity Id,
    string Firmware,
    BatteryInfo Battery,
    ReportRateDpi RateDpi,
    IReadOnlyList<DpiSlot> DpiSlots,
    IReadOnlyList<RgbColor> DpiColors,
    int LiftOff,
    PerformanceSettings Performance);

public enum ConnectionState { NoDevice, MouseAsleep, Connected }

/// <summary>
/// Owns the HID connection. All device I/O runs on a background thread, one call at a time.
/// Any I/O error drops the connection; the next call reconnects.
/// </summary>
public sealed class MouseService : IDisposable
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private CompxMouse? _mouse;
    private MouseIdentity? _id;

    public MouseIdentity? Identity => _id;

    /// <summary>Connects if needed and reads all settings.</summary>
    public async Task<(ConnectionState State, MouseSnapshot? Snapshot)> RefreshAllAsync()
    {
        return await RunAsync(() =>
        {
            var state = EnsureConnected();
            if (state != ConnectionState.Connected) return (state, null);

            var m = _mouse!;
            var snapshot = new MouseSnapshot(
                _id!, m.GetFirmwareVersion(), m.GetBattery(), m.GetReportRateDpi(),
                m.GetDpiSlots(), m.GetDpiColors(), m.GetLiftOffDistance(), m.GetPerformance());
            return (ConnectionState.Connected, (MouseSnapshot?)snapshot);
        });
    }

    public async Task<BatteryInfo?> GetBatteryAsync() =>
        await RunAsync(() => EnsureConnected() == ConnectionState.Connected ? _mouse!.GetBattery() : null);

    /// <summary>Runs a change on the mouse. Returns false if the mouse is not reachable.</summary>
    public async Task<bool> ApplyAsync(Action<CompxMouse, MouseIdentity> change) =>
        await RunAsync(() =>
        {
            if (EnsureConnected() != ConnectionState.Connected) return false;
            change(_mouse!, _id!);
            return true;
        });

    private async Task<T> RunAsync<T>(Func<T> work)
    {
        await _gate.WaitAsync();
        try
        {
            return await Task.Run(() =>
            {
                try
                {
                    return work();
                }
                catch
                {
                    Disconnect();
                    throw;
                }
            });
        }
        finally
        {
            _gate.Release();
        }
    }

    private ConnectionState EnsureConnected()
    {
        if (_mouse == null)
        {
            var dev = KnownDevices.Find().FirstOrDefault();
            if (dev == null) return ConnectionState.NoDevice;
            _mouse = new CompxMouse(new CompxTransport(dev.Hid));
            _id = null;
        }

        // A receiver stays plugged in while the mouse sleeps; check every time.
        if (!_mouse.IsOnline())
        {
            _id = null;
            return ConnectionState.MouseAsleep;
        }

        _id ??= _mouse.Handshake();
        return ConnectionState.Connected;
    }

    private void Disconnect()
    {
        _mouse?.Transport.Dispose();
        _mouse = null;
        _id = null;
    }

    public void Dispose()
    {
        Disconnect();
        _gate.Dispose();
    }
}
