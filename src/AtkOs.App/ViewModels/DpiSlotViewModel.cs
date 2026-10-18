using System.Windows.Input;
using AtkOs.Core;
using Avalonia.Media;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace AtkOs.App.ViewModels;

public sealed record ColorSwatch(string Hex, IBrush Brush, ICommand Command);

public partial class DpiSlotViewModel : ObservableObject
{
    /// <summary>Saturated colors: the DPI LED shows these best.</summary>
    private static readonly string[] PaletteHex =
        ["FF0000", "FF6A00", "FFD800", "00FF00", "00FFD0", "00A2FF", "0000FF", "8C00FF", "FF00DC", "FFFFFF"];

    private readonly MainViewModel _owner;
    private bool _silent;
    private CancellationTokenSource? _pending;

    public DpiSlotViewModel(MainViewModel owner, int index, int dpi, bool active, RgbColor color)
    {
        _owner = owner;
        Index = index;
        _dpi = dpi;
        _isActive = active;
        _color = ToBrush(color);
        _hex = color.ToString();
        Palette = PaletteHex
            .Select(h => new ColorSwatch("#" + h, new SolidColorBrush(Avalonia.Media.Color.Parse("#" + h)),
                new RelayCommand(() => PickColor(h))))
            .ToArray();
    }

    public int Index { get; }
    public string Label => $"DPI {Index + 1}";
    public IReadOnlyList<ColorSwatch> Palette { get; }

    [ObservableProperty] private decimal _dpi;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private IBrush _color;
    [ObservableProperty] private string _hex;

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

    /// <summary>Applies the color typed in the hex box (RRGGBB or #RRGGBB).</summary>
    [RelayCommand]
    private void ApplyHex() => PickColor(Hex);

    private void PickColor(string hex)
    {
        hex = hex.Trim().TrimStart('#');
        if (hex.Length != 6 || !Avalonia.Media.Color.TryParse("#" + hex, out var c)) return;
        var rgb = new RgbColor(c.R, c.G, c.B);
        Color = ToBrush(rgb);
        Hex = rgb.ToString();
        _ = _owner.ApplyColorAsync(Index, rgb);
    }

    private static IBrush ToBrush(RgbColor c) => new SolidColorBrush(Avalonia.Media.Color.FromRgb(c.R, c.G, c.B));

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
