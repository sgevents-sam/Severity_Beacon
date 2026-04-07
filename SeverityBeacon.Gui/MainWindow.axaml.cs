using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using SeverityBeacon.Gui.ViewModels;

namespace SeverityBeacon.Gui;

public partial class MainWindow : Window
{
    private bool _forceCloseForReopen;

    public MainWindow() : this(new MainWindowViewModel())
    {
    }

    public MainWindow(MainWindowViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
        Closing += OnClosing;
        Opened += async (_, _) =>
        {
            await ViewModel.InitializeAsync();
        };
    }

    public MainWindowViewModel ViewModel => (MainWindowViewModel)DataContext!;

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

    private async void TogglePollingClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.TogglePollingAsync();
    }

    private async void SaveProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.SaveProfileAsync();
    }

    private async void ProfileSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        await ViewModel.LoadSelectedProfileAsync();
    }

    private async void DeleteProfileClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.DeleteSelectedProfileAsync();
    }

    private async void ToggleSeverityOverrideClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        if (sender is Button { Tag: string severityName })
        {
            await ViewModel.ToggleSeverityOverrideAsync(severityName);
        }
    }

    private async void ManualOverrideChecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.SetManualOverrideEnabledAsync(true);
    }

    private async void ManualOverrideUnchecked(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        await ViewModel.SetManualOverrideEnabledAsync(false);
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

    private void OpenWebsiteClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Process.Start(new ProcessStartInfo
        {
            FileName = "https://sgeventservices.com",
            UseShellExecute = true
        });
    }

    private void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_forceCloseForReopen)
        {
            return;
        }

        if (Application.Current is App app && app.CanHideToMenuBar())
        {
            e.Cancel = true;
            Hide();
        }
    }

    private void HideGuiClick(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
    {
        Hide();
    }

    public void ForceCloseForReopen()
    {
        _forceCloseForReopen = true;
        Close();
    }

    public void ShowFromMenuBar()
    {
        ShowInTaskbar = true;
        IsVisible = true;
        Show();
        WindowState = WindowState.Normal;
        ActivateApplicationOnMac();
        Activate();
        Topmost = true;
        Topmost = false;
        Focus();
    }

    private static void ActivateApplicationOnMac()
    {
        if (!RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return;
        }

        try
        {
            var nsApplicationClass = objc_getClass("NSApplication");
            var sharedApplicationSelector = sel_registerName("sharedApplication");
            var application = objc_msgSend(nsApplicationClass, sharedApplicationSelector);
            var activateSelector = sel_registerName("activateIgnoringOtherApps:");
            objc_msgSend_bool(application, activateSelector, true);
        }
        catch
        {
            // Best-effort activation only.
        }
    }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_getClass")]
    private static extern IntPtr objc_getClass(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr sel_registerName(string selectorName);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr objc_msgSend(IntPtr receiver, IntPtr selector);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void objc_msgSend_bool(IntPtr receiver, IntPtr selector, bool value);

    public async Task TogglePollingFromMenuBarAsync()
    {
        await ViewModel.TogglePollingAsync();
    }
}
