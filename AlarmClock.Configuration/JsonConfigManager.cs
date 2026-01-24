using System;
using System.IO;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading;

namespace AlarmClock.Configuration;

public interface IConfigManager
{
    void Update<T>(T value);
    void Update<T>(string section, T value);
}

public class ConfigManager : IConfigManager
{
    private readonly string _path;
    private readonly JsonSerializerOptions? _opts;
    private readonly SemaphoreSlim _lock = new(1);

    public ConfigManager(string path, JsonSerializerOptions? options = null)
    {
        _path = path;
        _opts = options;
    }

    public void Update<T>(T value) => Update(ConfigurationMetadataProvider.GetPath<T>(), value);
    
    public void Update<T>(string section, T value)
    {
        _lock.Wait();
        try
        {
            var path = section.Split(':');
            var json = Load();

            foreach (var name in path[..^1])
            {
                if (!json.ContainsKey(name))
                    json.Add(name, json = new JsonObject());
                else
                    json = json[name]!.AsObject();
            }
        
            json[path[^1]] = JsonSerializer.SerializeToNode(value, _opts);

            Save(json);
        }
        finally
        {
            _lock.Release();
        }
    }

    private void Save(JsonObject json)
    {
        using var stream = File.Create(_path);

        JsonSerializer.Serialize(stream, json, _opts);
    }
    
    private JsonObject Load()
    {
        if (!Path.Exists(_path))
            return new JsonObject();
        
        using var stream = File.OpenRead(_path);
        return JsonSerializer.Deserialize<JsonObject>(stream, _opts)!;
    }
}