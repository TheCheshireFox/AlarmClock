using System;
using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using AlarmClock.Configuration;
using AlarmClock.Extensions;
using AlarmClock.ListProviders;
using AlarmClock.Radio;
using AlarmClock.Shared.Extensions;
using DynamicData;
using Microsoft.Extensions.Options;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class SettingsViewModel : ReactiveObject, IActivatableViewModel, IRoutableViewModel
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAlarmListProvider _alarmListProvider;
    private readonly IConfigManager _configManager;

    public ViewModelActivator Activator { get; }
    public string UrlPathSegment { get; } = nameof(SettingsViewModel);
    public IScreen HostScreen { get; }
    
    // source for combobox
    public ObservableCollection<BuzzerType> AlarmTypes
    {
        get;
        set => this.RaiseAndSetIfChanged(ref field, value);
    } = [];

    // selected item for combobox AlarmTypes
    public BuzzerType SelectedAlarmType
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

    public SettingsViewModel(IScreen screen, IRadioListProvider radioListProvider, IAlarmListProvider alarmListProvider, IOptionsMonitor<BuzzerConfiguration> buzzerConfiguration,
        IOptionsMonitor<RadioConfiguration> radioConfiguration, IConfigManager configManager)
    {
        _radioListProvider = radioListProvider;
        _alarmListProvider = alarmListProvider;
        _configManager = configManager;

        Activator = new ViewModelActivator();
        HostScreen = screen;
        
        this.WhenActivated(disposables =>
        {
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
                .WhereNotNull()
                .Subscribe(alarmType =>
                {
                    AlarmNames.Replace(alarmType switch
                    {
                        BuzzerType.Radio => _radioListProvider.Get().Keys,
                        BuzzerType.Sound => _alarmListProvider.Get().Keys,
                        _ => throw new ArgumentOutOfRangeException(nameof(alarmType), alarmType, null)
                    });
                    SelectedAlarmName = AlarmNames.Any() ? AlarmNames[0] : string.Empty;
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.SelectedAlarmName)
                .WhereNotNull()
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
                    }
        
                    _configManager.Update(config);
                })
                .DisposeWith(disposables);
            
            this.WhenAnyValue(x => x.SelectedRadioName)
                .WhereNotNull()
                .Subscribe(radioName =>
                {
                    _configManager.Update(radioConfiguration, x => x.Name = radioName);
                    _configManager.Update(buzzerConfiguration, x => x.Radio.Name = radioName);
                })
                .DisposeWith(disposables);
        });
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