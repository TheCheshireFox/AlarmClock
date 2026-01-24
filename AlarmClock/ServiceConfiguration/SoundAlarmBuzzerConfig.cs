using System;
using System.IO;
using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using Avalonia.Platform;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class SoundAlarmBuzzerConfig : ISoundAlarmBuzzerConfig
{
    private readonly IOptionsMonitor<BuzzerConfiguration> _options;
    
    public string SoundName => _options.CurrentValue.Sound.Name;
    
    public Stream? OpenSoundStream()
    {
        var uri = new Uri($"avares://AlarmClock/Assets/Sounds/{SoundName}.wav");
        return !AssetLoader.Exists(uri) ? null : AssetLoader.Open(uri);
    }

    public SoundAlarmBuzzerConfig(IOptionsMonitor<BuzzerConfiguration> options)
    {
        _options = options;
    }
}