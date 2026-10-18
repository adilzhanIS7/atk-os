using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;

namespace AtkOs.App.ViewModels;

public partial class DpiSlotViewModel : ObservableObject
{
    private readonly MainViewModel _owner;
    private bool _silent;
    private CancellationTokenSource? _pending;

    public DpiSlotViewModel(MainViewModel owner, int index, int dpi, bool active, IBrush color)
    {
        _owner = owner;
        Index = index;
        _dpi = dpi;
        _isActive = active;
        Color = color;
    }

    public int Index { get; }
    public string Label => $"DPI {Index + 1}";
    public IBrush Color { get; }

    [ObservableProperty] private decimal _dpi;
    [ObservableProperty] private bool _isActive;

    partial void OnIsActiveChanged(bool value)
    {
        if (!_silent && value) _owner.ActivateSlot(Index);
    }

    /// <summary>Waits until the user stops typing/clicking, then writes once.</summary>
    partial void OnDpiChanged(decimal value)
    {
        if (_silent) return;
        _pending?.Cancel();
        var cts = _pending = new CancellationTokenSource();
        _ = Task.Delay(600, cts.Token).ContinueWith(async _ =>
        {
            if (!cts.IsCancellationRequested) await _owner.ApplyDpiAsync(this);
        }, CancellationToken.None, TaskContinuationOptions.OnlyOnRanToCompletion, TaskScheduler.FromCurrentSynchronizationContext());
    }

    internal void SetActiveSilently(bool value)
    {
        _silent = true;
        IsActive = value;
        _silent = false;
    }

    internal void SetDpiSilently(int value)
    {
        _silent = true;
        Dpi = value;
        _silent = false;
    }
}
