using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using SeverityBeacon.Gui.ViewModels;

namespace SeverityBeacon.Gui;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();
    }

    public SettingsWindow(MainWindowViewModel viewModel) : this()
    {
        DataContext = viewModel;
    }

    private MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private async void RefreshPortsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.RefreshSerialPortsAsync();
    }

    private async void RefreshHostGroupsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.RefreshHostGroupsAsync();
    }

    private async void SaveProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.SaveProfileAsync();
    }

    private async void DeleteProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.DeleteSelectedProfileAsync();
    }

    private void RestoreAllColorsClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.RestoreAllColorDefaults();
    }

    private void RestoreDeskLampClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.RestoreDeskLampDefault();
    }

    private void RestoreAllClearClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        ViewModel.RestoreAllClearDefault();
    }

    private void RestoreSeverityClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: string severityName })
        {
            ViewModel.RestoreSeverityDefault(severityName);
        }
    }
}
