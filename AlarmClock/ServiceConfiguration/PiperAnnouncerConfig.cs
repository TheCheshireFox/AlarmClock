using System;
using AlarmClock.Announcer;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.ServiceConfiguration;

public class PiperAnnouncerConfig(IOptionsMonitor<AnnouncerConfiguration> options) : IPiperAnnouncerConfig
{
    public string Url => options.CurrentValue.Piper.Url;
    public bool Prefetch => options.CurrentValue.Piper.Prefetch;
    public TimeSpan Timeout => options.CurrentValue.Piper.Timeout;
}