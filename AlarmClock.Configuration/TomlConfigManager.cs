using Tomlyn;
using Tomlyn.Model;

namespace AlarmClock.Configuration;

public interface IConfigManager
{
    void Update<T>(T value) where T : notnull;
    void Update<T>(string section, T value) where T : notnull;
}

public class TomlConfigManager(string path, TomlSerializerOptions opts) : IConfigManager
{
    private readonly SemaphoreSlim _lock = new(1);

    public void Update<T>(T value) where T : notnull => Update(ConfigurationMetadataProvider.GetPath<T>(), value);
    
    public void Update<T>(string section, T value) where T : notnull
    {
        _lock.Wait();
        try
        {
            var pathSegments = section
                .Split(':')
                .Select(x => opts.PropertyNamingPolicy?.ConvertName(x) ?? x)
                .ToArray();
            
            var root = Load();
            var toml = root;

            foreach (var name in pathSegments[..^1])
            {
                if (!toml.ContainsKey(name))
                {
                    toml.Add(name, toml = new TomlTable());
                }
                else
                {
                    if (toml[name] is not TomlTable table)
                        throw new InvalidOperationException($"Invalid type for {name}");

                    toml = table;
                }
            }

            var serialized = TomlSerializer.Serialize(value, opts);
            toml[pathSegments[^1]] = TomlSerializer.Deserialize<TomlTable>(serialized, opts)
                                     ?? throw new InvalidOperationException($"Invalid type for {pathSegments[^1]}");

            Save(root);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Save(TomlTable toml)
    {
        using var stream = File.Create(path);

        TomlSerializer.Serialize(stream, toml, opts);
    }
    
    private TomlTable Load()
    {
        if (!Path.Exists(path))
            return new TomlTable();
        
        using var stream = File.OpenRead(path);
        return TomlSerializer.Deserialize<TomlTable>(stream, opts) ?? new TomlTable();
    }
}