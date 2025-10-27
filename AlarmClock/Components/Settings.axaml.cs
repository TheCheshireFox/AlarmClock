using System;
using System.Linq;
using AlarmClock.AlarmBuzzer;
using AlarmClock.Configuration;
using AlarmClock.DependencyInjection;
using AlarmClock.Design;
using AlarmClock.Extensions;
using AlarmClock.Radio;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Microsoft.Extensions.Options;

namespace AlarmClock.Components;

public partial class Settings : UserControl
{
    private bool _initialization;
    
    [Inject]
    private IRadioListProvider RadioListProvider { get; set; } = AppService.GetDefault<IRadioListProvider>();
    
    [Inject]
    private IAlarmListProvider AlarmListProvider { get; set; } = AppService.GetDefault<IAlarmListProvider>();
    
    [Inject]
    private IOptionsMonitor<BuzzerConfiguration> BuzzerConfiguration { get; set; } = AppService.GetDefault<IOptionsMonitor<BuzzerConfiguration>>();
    
    [Inject]
    private IOptionsMonitor<RadioConfiguration> RadioConfiguration { get; set; } = AppService.GetDefault<IOptionsMonitor<RadioConfiguration>>();
    
    [Inject]
    private IJsonConfigManager ConfigManager { get; set; } = AppService.GetDefault<IJsonConfigManager>();
    
    public Settings()
    {
        InitializeComponent();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);

        _initialization = true;
        
        AlarmType.ItemsSource = ConfigurationMetadataProvider.GetTypeVariants<BuzzerConfiguration>(x => x.Type);
        AlarmName.ItemsSource = AlarmListProvider.Get().Keys;
        RadioName.ItemsSource = RadioListProvider.Get().Keys;

        InitializeConfiguration();

        _initialization = false;
    }

    private void AlarmType_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_initialization || AlarmType == null)
            return;

        AlarmName.ItemsSource = AlarmType.SelectedItem switch
        {
            BuzzerType.Sound => AlarmListProvider.Get().Keys,
            BuzzerType.Radio => RadioListProvider.Get().Keys,
            _ => []
        };

        AlarmName.SelectedIndex = AlarmName.ItemsSource.Any() ? 0 : -1;
    }

    private void AlarmName_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_initialization || AlarmName == null)
            return;

        var config = BuzzerConfiguration.CurrentValue;
        
        switch (AlarmType.SelectedItem)
        {
            case BuzzerType.Sound:
                config.Type = BuzzerType.Sound;
                config.Sound.Name = AlarmName.SelectedItem?.ToString() ?? string.Empty;
                break;
            case BuzzerType.Radio:
                config.Type = BuzzerType.Radio;
                config.Radio.Name = AlarmName.SelectedItem?.ToString() ?? string.Empty;
                break;
        }
        
        ConfigManager.Update(config);
    }

    private void RadioName_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (_initialization || RadioName == null)
            return;

        var config = RadioConfiguration.CurrentValue;
        
        config.Name = RadioName.SelectedItem!.ToString() ?? string.Empty;

        ConfigManager.Update(config);
    }

    private void InitializeConfiguration()
    {
        var buzzerConfig = BuzzerConfiguration.CurrentValue;
        var radioConfig = RadioConfiguration.CurrentValue;
        
        switch (buzzerConfig.Type)
        {
            case BuzzerType.Radio:
                AlarmType.SelectedItem = BuzzerType.Radio;
                AlarmName.ItemsSource = RadioListProvider.Get().Keys;
                AlarmName.SelectedItem = buzzerConfig.Radio.Name;
                break;
            case BuzzerType.Sound:
                AlarmType.SelectedItem = BuzzerType.Sound;
                AlarmName.ItemsSource = AlarmListProvider.Get().Keys;
                AlarmName.SelectedItem = buzzerConfig.Sound.Name;
                break;
            default:
                AlarmType.SelectedItem = BuzzerType.Sound;
                AlarmName.ItemsSource = AlarmListProvider.Get().Keys;
                AlarmName.SelectedItem = string.Empty;
                break;
        }

        RadioName.SelectedItem = radioConfig.Name;
    }
}