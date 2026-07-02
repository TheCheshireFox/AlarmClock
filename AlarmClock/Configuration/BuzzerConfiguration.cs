namespace AlarmClock.Configuration;

public enum BuzzerType
{
    Sound,
    Radio
}

[ConfigurationPath("Buzzer")]
public class BuzzerConfiguration
{
    [TypeVariant(BuzzerType.Sound)]
    [TypeVariant(BuzzerType.Radio)]
    public BuzzerType Type { get; set; } = BuzzerType.Sound;
    public SoundBuzzerConfiguration Sound { get; set; } = new();
    public RadioBuzzerConfiguration Radio { get; set; } = new();
}

// type: sound
public class SoundBuzzerConfiguration
{
    public string Name { get; set; } = string.Empty;
}

// type: radio
public class RadioBuzzerConfiguration
{
    public string Name { get; set; } = string.Empty;
}