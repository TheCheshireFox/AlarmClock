using System;

namespace AlarmClock.Configuration;

[ConfigurationPath("Alarm")]
public class AlarmConfiguration
{
    public DateTime Time { get; set; }
    public bool Enabled { get; set; }
}