using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using AlarmClock.ViewModels;
using AlarmClock.Views;
using Avalonia;
using Avalonia.Interactivity;
using Avalonia.Threading;
using ReactiveUI;
using ReactiveUI.Avalonia;
using Timer = System.Timers.Timer;

namespace AlarmClock;

public partial class MainWindow : ReactiveWindow<MainWindowViewModel>
{
    private static readonly TimeSpan _menuTimeout = TimeSpan.FromSeconds(30);
    private readonly WakeShield _wakeShield = new();

    private readonly Timer _hideMenuTimer = new();
        
    public MainWindow()
    {
        InitializeComponent();
#if DEBUG
        this.AttachDevTools();
#endif

        this.WhenActivated(disposables =>
        {
            if (ViewModel == null)
                return;
            
            InitializeHideMenu();
            AddHandler(PointerPressedEvent, (_, _) => ResetHideMenu(), RoutingStrategies.Tunnel | RoutingStrategies.Bubble, true);

            ViewModel.NavBar.MenuVisible = false;
        
            _wakeShield.Attach(this);
            _wakeShield.OnWake
                .SelectMany(_ => Observable.FromAsync(ViewModel.DisplayOnAsync))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private void InitializeHideMenu()
    {
        _hideMenuTimer.AutoReset = false;
        _hideMenuTimer.Enabled = false;
        _hideMenuTimer.Interval = _menuTimeout.TotalMilliseconds;
        _hideMenuTimer.Elapsed += (_, _) =>
        {
            Dispatcher.UIThread.Invoke(() =>
            {
                ViewModel?.ResetPage();
                ViewModel?.NavBar.MenuVisible = false;
                _wakeShield.Activate();
                _hideMenuTimer.Stop();
            });
        };
    }

    private void ResetHideMenu()
    {
        ViewModel?.NavBar.MenuVisible = true;
        _hideMenuTimer.Stop();
        _hideMenuTimer.Start();
    }
}
