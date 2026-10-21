using Avalonia.Controls;
using Avalonia.Interactivity;

namespace AtkOs.App.Views;

public partial class MainWindow : Window
{
    public const string ProjectUrl = "https://github.com/adilzhanIS7/atk-os";

    public MainWindow() => InitializeComponent();

    private void OpenProjectPage(object? sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchUriAsync(new Uri(ProjectUrl));
}
