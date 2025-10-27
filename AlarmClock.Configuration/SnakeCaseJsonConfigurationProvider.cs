using Microsoft.Extensions.Configuration;

namespace AlarmClock.Configuration;

public class SnakeCaseJsonConfigurationProvider : FileConfigurationProvider
{
    public SnakeCaseJsonConfigurationProvider(FileConfigurationSource source) : base(source)
    {
    }
    
    public override void Load(Stream stream)
    {
        Data = SnakeCaseJsonDataLoader.Parse(stream, StringComparer.OrdinalIgnoreCase);
    }
}