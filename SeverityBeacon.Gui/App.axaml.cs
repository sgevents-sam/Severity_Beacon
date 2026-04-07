using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Controls.Shapes;
using Avalonia.Layout;
using Avalonia.Markup.Xaml;
using Avalonia.Media;
using Avalonia.Media.Imaging;
using Avalonia.Platform;
using Avalonia.Threading;
using SeverityBeacon.Gui.ViewModels;

namespace SeverityBeacon.Gui;

public partial class App : Application
{
    private const string TrayRoundalAsset = "avares://SeverityBeacon.Gui/tray-roundal.png";
    private readonly MainWindowViewModel _viewModel = new();
    private Bitmap? _trayOverlayBitmap;
    private MainWindow? _mainWindow;
    private TrayIcon? _trayIcon;
    private NativeMenuItem? _togglePollingItem;
    private bool _isQuitting;

    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;
            _viewModel.PropertyChanged += MainWindowViewModelOnPropertyChanged;
            _mainWindow = CreateMainWindow();
            desktop.MainWindow = _mainWindow;
            ConfigureTrayIcon();
            UpdateTrayState();
        }

        base.OnFrameworkInitializationCompleted();
    }

    public void ShowMainWindow()
    {
        Dispatcher.UIThread.Post(() =>
        {
            if (ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime desktop)
            {
                return;
            }

            if (_mainWindow != null)
            {
                try
                {
                    _mainWindow.ForceCloseForReopen();
                }
                catch
                {
                }
            }

            _mainWindow = CreateMainWindow();
            desktop.MainWindow = _mainWindow;
            _mainWindow.ShowFromMenuBar();
        });
    }

    public void BeginQuit()
    {
        _isQuitting = true;
        _trayIcon?.Dispose();
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.Shutdown();
        }
    }

    public bool CanHideToMenuBar()
    {
        return !_isQuitting;
    }

    private MainWindow CreateMainWindow()
    {
        return new MainWindow(_viewModel);
    }

    private void ConfigureTrayIcon()
    {
        using var overlayStream = AssetLoader.Open(new Uri(TrayRoundalAsset));
        _trayOverlayBitmap = new Bitmap(overlayStream);

        var menu = new NativeMenu();

        var showItem = new NativeMenuItem("Show Window");
        showItem.Click += (_, _) => ShowMainWindow();
        menu.Add(showItem);

        _togglePollingItem = new NativeMenuItem("Start Polling")
        {
            ToggleType = NativeMenuItemToggleType.CheckBox
        };
        _togglePollingItem.Click += async (_, _) =>
        {
            await _viewModel.TogglePollingAsync();
        };
        menu.Add(_togglePollingItem);

        menu.Add(new NativeMenuItemSeparator());

        var quitItem = new NativeMenuItem("Quit");
        quitItem.Click += (_, _) => BeginQuit();
        menu.Add(quitItem);

        _trayIcon = new TrayIcon
        {
            ToolTipText = "Severity Beacon",
            Menu = menu,
            IsVisible = true
        };
    }

    private void MainWindowViewModelOnPropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MainWindowViewModel.CurrentBeaconHex)
            or nameof(MainWindowViewModel.IsRunning))
        {
            Dispatcher.UIThread.Post(UpdateTrayState);
        }
    }

    private void UpdateTrayState()
    {
        if (_trayIcon == null)
        {
            return;
        }

        _trayIcon.Icon = BuildTrayIcon(_viewModel.CurrentBeaconColor);
        _trayIcon.IsVisible = true;

        if (_togglePollingItem != null)
        {
            _togglePollingItem.IsChecked = _viewModel.IsRunning;
            _togglePollingItem.Header = _viewModel.IsRunning ? "Stop Polling" : "Start Polling";
        }
    }

    private WindowIcon BuildTrayIcon(Color beaconColor)
    {
        const int width = 88;
        const int height = 56;
        const int inset = 6;

        using var renderTarget = new RenderTargetBitmap(new PixelSize(width, height), new Vector(96, 96));
        var canvas = new Grid
        {
            Width = width,
            Height = height,
            Background = Brushes.Transparent
        };

        canvas.Children.Add(new Border
        {
            Width = width,
            Height = height,
            CornerRadius = new CornerRadius(12),
            Background = new SolidColorBrush(beaconColor),
            BorderBrush = new SolidColorBrush(Color.Parse("#24122F")),
            BorderThickness = new Thickness(1)
        });

        if (_trayOverlayBitmap != null)
        {
            canvas.Children.Add(new Image
            {
                Source = _trayOverlayBitmap,
                Width = width - (inset * 2),
                Height = height - (inset * 2),
                Margin = new Thickness(inset),
                Stretch = Stretch.Uniform
            });
        }

        canvas.Measure(new Size(width, height));
        canvas.Arrange(new Rect(0, 0, width, height));
        renderTarget.Render(canvas);

        using var stream = new MemoryStream();
        renderTarget.Save(stream);
        stream.Position = 0;
        return new WindowIcon(stream);
    }
}
