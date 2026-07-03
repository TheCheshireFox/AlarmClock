using System;
using System.IO;
using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using Avalonia.Platform;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class SoundAlarmBuzzerConfig(IOptionsMonitor<BuzzerConfiguration> options) : ISoundAlarmBuzzerConfig
{
    public string SoundName => options.CurrentValue.Sound.Name;
    
    public Stream? OpenSoundStream()
    {
        var uri = new Uri($"avares://AlarmClock/Assets/Sounds/{SoundName}.wav");
        return !AssetLoader.Exists(uri) ? null : AssetLoader.Open(uri);
    }
}