using Microsoft.Extensions.Configuration;

namespace AlarmClock.Configuration.Toml;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddTomlFile(
        this IConfigurationBuilder builder,
        Action<TomlWithOptionsConfigurationSource> configureSource)
    {
        builder.Add(configureSource);
        return builder;
    }
}