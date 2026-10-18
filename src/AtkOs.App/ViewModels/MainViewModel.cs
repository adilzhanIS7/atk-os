using System.Collections.ObjectModel;
using AtkOs.App.Services;
using AtkOs.Core;
using AtkOs.Core.Protocol;
using Avalonia.Media;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtkOs.App.ViewModels;

public sealed record RateOption(ReportRate Rate, string Label);
public sealed record LodOption(byte Raw, string Label);
public sealed record DebounceOption(int Ms, string Label);

public partial class MainViewModel : ObservableObject
{
    private static readonly TimeSpan SearchInterval = TimeSpan.FromSeconds(3);
    private static readonly TimeSpan BatteryInterval = TimeSpan.FromMinutes(1);

    private readonly MouseService _service;
    private readonly DispatcherTimer _timer = new();
    private MouseSnapshot? _snapshot;
    private DpiSlot[] _allSlots = new DpiSlot[8];
    private RgbColor[] _allColors = new RgbColor[8];

    /// <summary>True while values are being loaded from the mouse, so they are not written back.</summary>
    private bool _loading;

    public MainViewModel(MouseService service)
    {
        _service = service;
        _timer.Tick += async (_, _) => await TickAsync();
        _timer.Interval = SearchInterval;
        _timer.Start();
        _ = ReloadAsync();
    }

    /// <summary>Fires when the battery level or connection changes (for the tray icon).</summary>
    public event Action? BatteryChanged;

    // ---------- Header ----------

    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _status = "Searching for a mouse…";
    [ObservableProperty] private string _modelName = "No mouse";
    [ObservableProperty] private string _details = "";
    [ObservableProperty] private int? _batteryPercent;
    [ObservableProperty] private bool _charging;

    public string BatteryText => BatteryPercent is { } p ? $"{p}%{(Charging ? " ⚡" : "")}" : "—";

    partial void OnBatteryPercentChanged(int? value) => OnPropertyChanged(nameof(BatteryText));
    partial void OnChargingChanged(bool value) => OnPropertyChanged(nameof(BatteryText));

    // ---------- DPI ----------

    public ObservableCollection<DpiSlotViewModel> DpiSlots { get; } = [];

    [ObservableProperty] private decimal _slotCount = 1;
    [ObservableProperty] private int _dpiStep = 50;
    [ObservableProperty] private int _maxDpi = 26000;

    partial void OnSlotCountChanged(decimal value)
    {
        RebuildSlots();
        if (_loading) return;
        var count = (int)value;
        _ = ApplyAsync(m =>
        {
            m.SetDpiSlotCount(count);
            // Keep the active slot inside the cycle.
            if (m.GetReportRateDpi().CurrentDpiSlot >= count) m.SetCurrentDpiSlot(count - 1);
        });
    }

    internal void ActivateSlot(int index)
    {
        foreach (var s in DpiSlots) s.SetActiveSilently(s.Index == index);
        if (!_loading) _ = ApplyAsync(m => m.SetCurrentDpiSlot(index));
    }

    internal async Task ApplyDpiAsync(DpiSlotViewModel slot)
    {
        var id = _service.Identity;
        if (id == null) return;
        var dpi = DpiCodec.Nearest((int)slot.Dpi, id.DpiEncoding, id.MaxDpi);
        if (dpi != (int)slot.Dpi) slot.SetDpiSilently(dpi);

        var encoded = DpiCodec.Encode(dpi, id.DpiEncoding);
        _allSlots[slot.Index] = encoded;
        await ApplyAsync(m => m.SetDpiSlot(slot.Index, encoded));
    }

    private void RebuildSlots()
    {
        var id = _snapshot?.Id;
        if (id == null) return;
        int active = DpiSlots.FirstOrDefault(s => s.IsActive)?.Index ?? _snapshot!.RateDpi.CurrentDpiSlot;
        active = Math.Min(active, (int)SlotCount - 1);

        DpiSlots.Clear();
        for (int i = 0; i < (int)SlotCount; i++)
        {
            var (x, _) = DpiCodec.Decode(_allSlots[i], id.DpiEncoding);
            var c = _allColors[i];
            DpiSlots.Add(new DpiSlotViewModel(this, i, x, i == active,
                new SolidColorBrush(Color.FromRgb(c.R, c.G, c.B))));
        }
    }

    // ---------- Polling rate ----------

    public ObservableCollection<RateOption> ReportRates { get; } = [];
    [ObservableProperty] private RateOption? _selectedRate;

    partial void OnSelectedRateChanged(RateOption? value)
    {
        if (!_loading && value != null) _ = ApplyAsync(m => m.SetReportRate(value.Rate));
    }

    // ---------- Sensor ----------

    public LodOption[] LodOptions { get; } = Options.LiftOff.Select(o => new LodOption(o.Raw, o.Label)).ToArray();
    [ObservableProperty] private LodOption? _selectedLod;

    partial void OnSelectedLodChanged(LodOption? value)
    {
        if (!_loading && value != null) _ = ApplyAsync(m => m.SetLiftOffDistance(value.Raw));
    }

    public DebounceOption[] DebounceOptions { get; } = Options.DebounceMs.Select(ms => new DebounceOption(ms, $"{ms} ms")).ToArray();
    [ObservableProperty] private DebounceOption? _selectedDebounce;

    partial void OnSelectedDebounceChanged(DebounceOption? value)
    {
        if (!_loading && value != null) _ = ApplyAsync(m => m.SetDebounce(value.Ms));
    }

    [ObservableProperty] private bool _motionSync;
    [ObservableProperty] private bool _angleSnapping;
    [ObservableProperty] private bool _rippleControl;

    partial void OnMotionSyncChanged(bool value) => Toggle(PerformanceToggle.MotionSync, value);
    partial void OnAngleSnappingChanged(bool value) => Toggle(PerformanceToggle.AngleSnapping, value);
    partial void OnRippleControlChanged(bool value) => Toggle(PerformanceToggle.RippleControl, value);

    private void Toggle(PerformanceToggle t, bool on)
    {
        if (!_loading) _ = ApplyAsync(m => m.SetToggle(t, on));
    }

    // ---------- Device I/O ----------

    private async Task ApplyAsync(Action<CompxMouse> change)
    {
        try
        {
            if (!await _service.ApplyAsync((m, _) => change(m)))
                await ReloadAsync();
            else
                Status = "Saved to mouse";
        }
        catch (Exception e)
        {
            Status = $"Error: {e.Message}";
            await ReloadAsync();
        }
    }

    /// <summary>Re-reads everything from the mouse (also called when the window is shown).</summary>
    public async Task ReloadAsync()
    {
        try
        {
            var (state, snapshot) = await _service.RefreshAllAsync();
            if (snapshot == null)
            {
                SetDisconnected(state == ConnectionState.MouseAsleep
                    ? "Receiver found — the mouse is off or asleep"
                    : "Searching for a mouse…");
                return;
            }
            Load(snapshot);
        }
        catch (Exception e)
        {
            SetDisconnected($"Connection lost: {e.Message}");
        }
    }

    private async Task TickAsync()
    {
        if (!IsConnected)
        {
            await ReloadAsync();
            return;
        }
        try
        {
            var battery = await _service.GetBatteryAsync();
            if (battery == null) SetDisconnected("Receiver found — the mouse is off or asleep");
            else SetBattery(battery);
        }
        catch (Exception e)
        {
            SetDisconnected($"Connection lost: {e.Message}");
        }
    }

    private void SetDisconnected(string status)
    {
        var changed = IsConnected || BatteryPercent != null;
        IsConnected = false;
        Status = status;
        ModelName = "No mouse";
        Details = "";
        BatteryPercent = null;
        _timer.Interval = SearchInterval;
        if (changed) BatteryChanged?.Invoke();
    }

    private void SetBattery(BatteryInfo b)
    {
        var changed = BatteryPercent != b.Percent || Charging != b.Charging;
        BatteryPercent = b.Percent;
        Charging = b.Charging;
        if (changed) BatteryChanged?.Invoke();
    }

    private void Load(MouseSnapshot s)
    {
        _loading = true;
        try
        {
            _snapshot = s;
            _allSlots = [.. s.DpiSlots];
            _allColors = [.. s.DpiColors];

            IsConnected = true;
            Status = "Connected";
            ModelName = s.Id.Model.Title;
            Details = $"{ConnectionLabel(s.Id.Connection)} · firmware {s.Firmware}";
            MaxDpi = s.Id.MaxDpi;
            DpiStep = s.Id.DpiEncoding == DpiEncoding.Step10 ? 10 : 50;

            ReportRates.Clear();
            foreach (var r in Options.ReportRatesFor(s.Id.Connection).Reverse())
                ReportRates.Add(new RateOption(r, r.ToHz() >= 1000 ? $"{r.ToHz() / 1000}K" : $"{r.ToHz()}"));
            SelectedRate = ReportRates.FirstOrDefault(r => r.Rate == s.RateDpi.ReportRate);

            SlotCount = Math.Clamp(s.RateDpi.DpiSlotCount, 1, 8);
            DpiSlots.Clear();
            RebuildSlots();

            SelectedLod = LodOptions.FirstOrDefault(o => o.Raw == s.LiftOff);
            SelectedDebounce = DebounceOptions.FirstOrDefault(o => o.Ms == s.Performance.Debounce);
            MotionSync = s.Performance.MotionSync != 0;
            AngleSnapping = s.Performance.AngleSnapping != 0;
            RippleControl = s.Performance.RippleControl != 0;

            SetBattery(s.Battery);
            _timer.Interval = BatteryInterval;
        }
        finally
        {
            _loading = false;
        }
    }

    private static string ConnectionLabel(ConnectionType c) => c switch
    {
        ConnectionType.Dongle1K => "2.4G 1K",
        ConnectionType.Dongle2K => "2.4G 2K",
        ConnectionType.Dongle4K => "2.4G 4K",
        ConnectionType.Dongle8K => "2.4G 8K",
        ConnectionType.Wired1K => "Wired 1K",
        ConnectionType.Wired8K => "Wired 8K",
        _ => c.ToString(),
    };
}
