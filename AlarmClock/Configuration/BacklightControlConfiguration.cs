using System;
using System.Diagnostics.CodeAnalysis;

namespace AlarmClock.Configuration;

public class SchedulerBrightnessPolicy
{
    public TimeSpan DimStart { get; set; } = TimeSpan.Zero;
    public TimeSpan DimStop { get; set; } = TimeSpan.Zero;
    public double DimBrightness { get; set; } = 0.2;
}

// Ambient light sensor, GPIO, TODO
public class AlsBrightnessPolicy
{
}

[SuppressMessage("ReSharper", "InconsistentNaming")]
public enum BacklightControlPolicy
{
    None,
    ALS,
    Scheduled
}

[ConfigurationPath("Backlight")]
public class BacklightControlConfiguration
{
    [TypeVariant(nameof(BacklightControlPolicy.None))]
    [TypeVariant(nameof(BacklightControlPolicy.ALS))]
    [TypeVariant(nameof(BacklightControlPolicy.Scheduled))]
    public BacklightControlPolicy Policy { get; set; } = BacklightControlPolicy.None;
    public TimeSpan? DimTimeout { get; set; }
    public double DimLevel { get; set; } = 0.5;
    public TimeSpan? StandbyTimeout { get; set; }
    public SchedulerBrightnessPolicy SchedulerPolicy { get; set; } = new();
    public AlsBrightnessPolicy AlsPolicy { get; set; } = new();
}