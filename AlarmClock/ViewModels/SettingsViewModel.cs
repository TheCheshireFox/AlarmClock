using System;
using System.Collections.ObjectModel;
using System.Reactive;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Configuration;
using AlarmClock.Extensions;
using AlarmClock.ListProviders;
using AlarmClock.Network;
using AlarmClock.Radio;
using AlarmClock.Shared.Extensions;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class SettingsViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAlarmListProvider _alarmListProvider;
    private readonly IWiFiManager _wiFiManager;

    public ViewModelActivator Activator { get; }
    public string UrlPathSegment { get; } = nameof(SettingsViewModel);
    public IScreen HostScreen { get; }

    public ReactiveCommand<Unit, IRoutableViewModel> OpenWiFiSettings { get; }
    public ReactiveCommand<Unit, Unit> RefreshWiFiStatus { get; }

    public string WiFiSsid
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public string WiFiStatus
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = "UNKNOWN";
    
    // source for combobox
    public ObservableCollection<BuzzerType> AlarmTypes
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    // selected item for combobox AlarmTypes
    public BuzzerType? SelectedAlarmType
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    }

    // source for combobox
    public ObservableCollection<string> AlarmNames
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    // selected item for combobox AlarmNames
    public string SelectedAlarmName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    // source for combobox
    public ObservableCollection<string> RadioNames
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    // selected item for combobox RadioNames
    public string SelectedRadioName
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = string.Empty;

    public SettingsViewModel(INavigationHost screen, IRadioListProvider radioListProvider, IAlarmListProvider alarmListProvider, IOptionsMonitor<BuzzerConfiguration> buzzerConfiguration,
        IOptionsMonitor<RadioConfiguration> radioConfiguration, IConfigManager configManager, IWiFiManager wiFiManager)
    {
        _radioListProvider = radioListProvider;
        _alarmListProvider = alarmListProvider;
        _wiFiManager = wiFiManager;

        Activator = new ViewModelActivator();
        HostScreen = screen;
        OpenWiFiSettings = ReactiveCommand.CreateFromObservable(screen.NavigateTo<WiFiSettingsViewModel>);
        RefreshWiFiStatus = ReactiveCommand.CreateFromTask(RefreshWiFiStatusAsync);
        
        this.WhenActivated(disposables =>
        {
            RefreshWiFiStatus.ThrownExceptions
                .Subscribe(_ =>
                {
                    WiFiSsid = "NONE";
                    WiFiStatus = "UNKNOWN";
                })
                .DisposeWith(disposables);

            RefreshWiFiStatus.Execute()
                .Subscribe()
                .DisposeWith(disposables);

            AlarmTypes.Replace(ConfigurationMetadataProvider.GetTypeVariants<BuzzerConfiguration, BuzzerType>(x => x.Type));
            RadioNames.Replace(_radioListProvider.Get().Keys);

            buzzerConfiguration
                .Subscribe(OnBuzzerConfigurationChanged)
                .DisposeWith(disposables);

            radioConfiguration
                .Subscribe(OnRadioConfigurationChanged)
                .DisposeWith(disposables);

            OnBuzzerConfigurationChanged(buzzerConfiguration.CurrentValue);
            OnRadioConfigurationChanged(radioConfiguration.CurrentValue);

            this.WhenAnyValue(x => x.SelectedAlarmType)
                .Skip(1)
                .WhereNotNull()
                .DistinctUntilChanged()
                .Subscribe(alarmType =>
                {
                    AlarmNames.Replace(alarmType switch
                    {
                        BuzzerType.Radio => _radioListProvider.Get().Keys,
                        BuzzerType.Sound => _alarmListProvider.Get().Keys,
                        BuzzerType.Gpio => [],
                        _ => throw new ArgumentOutOfRangeException(nameof(alarmType), alarmType, null)
                    });
                    SelectedAlarmName = AlarmNames.Any() ? AlarmNames[0] : string.Empty;
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.SelectedAlarmName)
                .Skip(1)
                .WhereNotNull()
                .DistinctUntilChanged()
                .Subscribe(alarmName =>
                {
                    var config = buzzerConfiguration.CurrentValue;
        
                    switch (SelectedAlarmType)
                    {
                        case BuzzerType.Sound:
                            config.Type = BuzzerType.Sound;
                            config.Sound.Name = alarmName;
                            break;
                        case BuzzerType.Radio:
                            config.Type = BuzzerType.Radio;
                            config.Radio.Name = alarmName;
                            break;
                        case BuzzerType.Gpio:
                            config.Type = BuzzerType.Gpio;
                            break;
                    }
        
                    configManager.Update(config);
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.SelectedRadioName)
                .Skip(1)
                .WhereNotNull()
                .DistinctUntilChanged()
                .Subscribe(radioName =>
                {
                    configManager.Update(radioConfiguration, x => x.Name = radioName);
                })
                .DisposeWith(disposables);
        });
    }

    private async Task RefreshWiFiStatusAsync(CancellationToken cancellationToken)
    {
        var status = await _wiFiManager.GetConnectionStatusAsync(cancellationToken);

        WiFiSsid = string.IsNullOrWhiteSpace(status.Ssid) ? "NONE" : status.Ssid;
        WiFiStatus = status.Connected ? "CONNECTED" : "DISCONNECTED";
    }
    
    private void OnBuzzerConfigurationChanged(BuzzerConfiguration config)
    {
        switch (config.Type)
        {
            case BuzzerType.Radio:
                SelectedAlarmType = BuzzerType.Radio;
                AlarmNames.Replace(_radioListProvider.Get().Keys);
                SelectedAlarmName = config.Radio.Name;
                break;
            case BuzzerType.Sound:
                SelectedAlarmType = BuzzerType.Sound;
                AlarmNames.Replace(_alarmListProvider.Get().Keys);
                SelectedAlarmName = config.Sound.Name;
                break;
            case BuzzerType.Gpio:
                SelectedAlarmType = BuzzerType.Gpio;
                AlarmNames.Clear();
                SelectedAlarmName = string.Empty;
                break;
            default:
                SelectedAlarmType = BuzzerType.Sound;
                AlarmNames.Replace(_alarmListProvider.Get().Keys);
                SelectedAlarmName = string.Empty;
                break;
        }
    }

    private void OnRadioConfigurationChanged(RadioConfiguration config)
    {
        SelectedRadioName = config.Name;
    }
}
