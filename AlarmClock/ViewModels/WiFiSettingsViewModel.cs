using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using AlarmClock.Extensions;
using AlarmClock.Network;
using DynamicData;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class WiFiSettingsViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    private readonly IWiFiManager _wiFiManager;
    private string _selectedSsid = string.Empty;

    public ViewModelActivator Activator { get; } = new();
    public string UrlPathSegment { get; } = nameof(WiFiSettingsViewModel);
    public IScreen HostScreen { get; }

    public ObservableCollection<string> AccessPoints { get; } = [];

    public ReactiveCommand<Unit, Unit> RefreshNetworks { get; }
    public ReactiveCommand<Unit, Unit> Connect { get; }
    public ReactiveCommand<Unit, Unit> ShowKeyboard { get; }
    public ReactiveCommand<string, Unit> HideKeyboard { get; }
    public ICommand Back { get; }

    public string SelectedSsid
    {
        get => _selectedSsid;
        set => SetSelectedSsid(value, true);
    }

    public string Password
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public bool IsPasswordVisible
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsPasswordHidden));
        }
    }

    public bool IsPasswordHidden => !IsPasswordVisible;

    public bool IsKeyboardVisible
    {
        get;
        set
        {
            this.RaiseAndSetIfChanged(ref field, value);
            this.RaisePropertyChanged(nameof(IsNetworkListVisible));
        }
    }

    public bool IsNetworkListVisible => !IsKeyboardVisible;

    public bool IsBusy
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    public string StatusMessage
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "Tap Refresh to scan";

    public WiFiSettingsViewModel(IScreen screen, IWiFiManager wiFiManager)
    {
        HostScreen = screen;
        _wiFiManager = wiFiManager;

        RefreshNetworks = ReactiveCommand.CreateFromTask(RefreshNetworksAsync);
        Connect = ReactiveCommand.CreateFromTask(ConnectAsync, this.WhenAnyValue(x => x.SelectedSsid, x => x.IsBusy, CanConnect));
        ShowKeyboard = ReactiveCommand.Create(() => { IsKeyboardVisible = true; });
        HideKeyboard = ReactiveCommand.Create<string>(_ => IsKeyboardVisible = false);
        Back = ReactiveCommand.CreateFromObservable(() => HostScreen.Router.NavigateBack.Execute());

        this.WhenActivated(disposables =>
        {
            RefreshNetworks.ThrownExceptions
                .Subscribe(SetError)
                .DisposeWith(disposables);

            Connect.ThrownExceptions
                .Subscribe(SetError)
                .DisposeWith(disposables);

            RefreshNetworks.Execute()
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private static bool CanConnect(string ssid, bool isBusy)
    {
        return !isBusy && !string.IsNullOrWhiteSpace(ssid);
    }

    private async Task RefreshNetworksAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        StatusMessage = "Scanning...";

        try
        {
            var accessPoints = await _wiFiManager.ListAccessPointsAsync(cancellationToken);
            var connectionStatus = await _wiFiManager.GetConnectionStatusAsync(cancellationToken);
            var selected = SelectedSsid;
            var connectedSsid = connectionStatus.Connected ? connectionStatus.Ssid : string.Empty;

            AccessPoints.Replace(accessPoints);

            if (!string.IsNullOrWhiteSpace(connectedSsid) && !AccessPoints.Contains(connectedSsid))
            {
                AccessPoints.Add(connectedSsid);
            }

            var selectedSsid = !string.IsNullOrWhiteSpace(connectedSsid)
                ? connectedSsid
                : AccessPoints.Contains(selected)
                    ? selected
                    : string.Empty;

            SetSelectedSsid(selectedSsid, false);
            StatusMessage = connectionStatus.Connected
                ? $"Connected to {connectionStatus.Ssid}"
                : AccessPoints.Count == 0
                    ? "No networks found"
                    : $"{AccessPoints.Count} networks found";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private async Task ConnectAsync(CancellationToken cancellationToken)
    {
        IsBusy = true;
        StatusMessage = $"Connecting to {SelectedSsid}...";

        try
        {
            IsKeyboardVisible = false;

            var connected = await _wiFiManager.ConnectAsync(SelectedSsid, Password, cancellationToken);
            StatusMessage = connected ? $"Connected to {SelectedSsid}" : $"Failed to connect to {SelectedSsid}";
        }
        finally
        {
            IsBusy = false;
        }
    }

    private void SetError(Exception error)
    {
        IsBusy = false;
        StatusMessage = error.Message;
    }

    private void SetSelectedSsid(string? value, bool showPasswordEntry)
    {
        var ssid = value ?? string.Empty;
        var ssidChanged = _selectedSsid != ssid;

        this.RaiseAndSetIfChanged(ref _selectedSsid, ssid, nameof(SelectedSsid));

        if (string.IsNullOrWhiteSpace(ssid))
        {
            IsKeyboardVisible = false;
        }
        else if (ssidChanged && showPasswordEntry)
        {
            Password = string.Empty;
            IsKeyboardVisible = true;
            StatusMessage = $"Password for {ssid}";
        }
    }
}
