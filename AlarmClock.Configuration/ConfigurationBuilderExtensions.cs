using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.FileProviders;

namespace AlarmClock.Configuration;

public static class ConfigurationBuilderExtensions
{
    public static IConfigurationBuilder AddSnakeCaseJsonFile(
        this IConfigurationBuilder builder,
        string path,
        bool optional = false,
        bool reloadOnChange = false,
        IFileProvider? fileProvider = null)
    {
        ArgumentNullException.ThrowIfNull(builder);
        
        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException("Path is required.", nameof(path));

        var src = new SnakeCaseJsonConfigurationSource
        {
            FileProvider = fileProvider,
            Path = path,
            Optional = optional,
            ReloadOnChange = reloadOnChange
        };
        src.ResolveFileProvider();

        return builder.Add(src);
    }
}