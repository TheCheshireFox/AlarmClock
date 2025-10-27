using System.Diagnostics.CodeAnalysis;

namespace AlarmClock.Configuration;

public class PwmDisplayControllerConfiguration
{
    public int Pin { get; set; } = 0;
    public int Frequency { get; set; } = 1000;
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum DisplayControllerType
{
    None,
    PWM
}

[ConfigurationPath("Display")]
public class DisplayControllerConfiguration
{
    [TypeVariant(nameof(DisplayControllerType.None))]
    [TypeVariant(nameof(DisplayControllerType.PWM))]
    public DisplayControllerType Type { get; set; } = DisplayControllerType.None;
    public PwmDisplayControllerConfiguration Pwm { get; set; } = new ();
}