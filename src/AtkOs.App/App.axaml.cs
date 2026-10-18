using AtkOs.App.Services;
using AtkOs.App.ViewModels;
using AtkOs.App.Views;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;

namespace AtkOs.App;

public partial class App : Application
{
    private readonly MouseService _service = new();
    private MainViewModel? _vm;
    private MainWindow? _window;
    private TrayIcon? _tray;

    public override void Initialize() => AvaloniaXamlLoader.Load(this);

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            _vm = new MainViewModel(_service);
            _vm.BatteryChanged += UpdateTray;
            _vm.LowBattery += ShowLowBattery;

            CreateTray(desktop);
            if (!Program.StartInTray) ShowWindow();
            else MemoryTrimmer.Trim(5000);

            if (Program.TestNotification) ShowLowBattery(15);

            desktop.Exit += (_, _) => _service.Dispose();
        }
        base.OnFrameworkInitializationCompleted();
    }

    private void CreateTray(IClassicDesktopStyleApplicationLifetime desktop)
    {
        var open = new NativeMenuItem("Open");
        open.Click += (_, _) => ShowWindow();
        var quit = new NativeMenuItem("Quit");
        quit.Click += (_, _) => desktop.Shutdown();

        _tray = new TrayIcon
        {
            Icon = TrayIconRenderer.Render(null, false),
            ToolTipText = "ATK-OS",
            Menu = [open, new NativeMenuItemSeparator(), quit],
            IsVisible = true,
        };
        _tray.Clicked += (_, _) => ShowWindow();
        TrayIcon.SetIcons(this, [_tray]);
    }

    private void UpdateTray()
    {
        if (_tray == null || _vm == null) return;
        _tray.Icon = TrayIconRenderer.Render(_vm.BatteryPercent, _vm.Charging);
        _tray.ToolTipText = _vm.IsConnected
            ? $"{_vm.ModelName} — {_vm.BatteryText}"
            : "ATK-OS — no mouse";
        if (_window != null) _window.Icon = _tray.Icon;
    }

    private void ShowLowBattery(int percent) =>
        NotificationPopup.Show(
            $"Mouse battery low: {percent}%",
            $"{(_vm is { IsConnected: true } ? _vm.ModelName : "Your mouse")} needs charging soon.",
            Avalonia.Media.Color.Parse(percent <= 10 ? "#D64545" : "#E0A030"));

    private void ShowWindow()
    {
        if (_window == null)
        {
            _window = new MainWindow { DataContext = _vm, Icon = _tray?.Icon };
            // Closing the window keeps the app in the tray. The window is really closed
            // (not hidden) so its visuals can be freed; a new one is made on the next open.
            _window.Closed += (_, _) =>
            {
                _window = null;
                MemoryTrimmer.Trim();
            };
        }
        _window.Show();
        _window.Activate();
        // The DPI button on the mouse may have changed things while hidden.
        _ = _vm?.ReloadAsync();
    }
}
