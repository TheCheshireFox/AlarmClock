using System;
using AlarmClock.Announcer;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class PiperAnnouncerConfig : IPiperAnnouncerConfig
{
    private readonly IOptionsMonitor<AnnouncerConfiguration> _options;

    public string Url => _options.CurrentValue.Piper.Url;
    public bool Prefetch => _options.CurrentValue.Piper.Prefetch;
    public TimeSpan Timeout => _options.CurrentValue.Piper.Timeout;
    
    public PiperAnnouncerConfig(IOptionsMonitor<AnnouncerConfiguration> options)
    {
        _options = options;
    }
}