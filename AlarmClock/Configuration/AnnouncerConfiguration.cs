namespace AlarmClock.Configuration;

public enum AnnouncerType
{
    Silent,
    Piper
}

[ConfigurationPath("Announcer")]
public class AnnouncerConfiguration
{
    [TypeVariant(nameof(AnnouncerType.Silent))]
    [TypeVariant(nameof(AnnouncerType.Piper))]
    public AnnouncerType Type { get; set; } = AnnouncerType.Silent;
    public PiperAnnouncerConfiguration Piper { get; set; } = new();
}

public class PiperAnnouncerConfiguration
{
    public string Url { get; set; } = string.Empty;
    public bool Prefetch { get; set; } = false;
}