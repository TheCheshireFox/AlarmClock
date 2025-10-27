using Microsoft.Extensions.Configuration;

namespace AlarmClock.Configuration;

public class SnakeCaseJsonConfigurationSource : FileConfigurationSource
{
    public override IConfigurationProvider Build(IConfigurationBuilder builder)
    {
        EnsureDefaults(builder);
        return new SnakeCaseJsonConfigurationProvider(this);
    }
}