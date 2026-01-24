using System;

namespace AlarmClock.Configuration;

public enum AnnouncerType
{
    Silent,
    Piper
}

[ConfigurationPath("Announcer")]
public class AnnouncerConfiguration
{
    [TypeVariant(AnnouncerType.Silent)]
    [TypeVariant(AnnouncerType.Piper)]
    public AnnouncerType Type { get; set; } = AnnouncerType.Silent;
    public PiperAnnouncerConfiguration Piper { get; set; } = new();
}

public class PiperAnnouncerConfiguration
{
    public string Url { get; set; } = string.Empty;
    public bool Prefetch { get; set; } = false;
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(15);
}