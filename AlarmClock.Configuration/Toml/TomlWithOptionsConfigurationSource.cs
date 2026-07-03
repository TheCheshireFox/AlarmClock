using Microsoft.Extensions.Configuration;
using Tomlyn;

namespace AlarmClock.Configuration.Toml;

public class TomlWithOptionsConfigurationSource : FileConfigurationSource
{
    public TomlSerializerOptions? Options { get; set; }
    
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new TomlWithOptionsConfigurationProvider(this, Options);
    }
}