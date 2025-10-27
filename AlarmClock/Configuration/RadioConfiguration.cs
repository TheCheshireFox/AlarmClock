namespace AlarmClock.Configuration;

[ConfigurationPath("radio")]
public class RadioConfiguration
{
    public required string Name { get; set; } = string.Empty;
}