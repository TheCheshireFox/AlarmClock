using System;
using System.IO;

namespace AlarmClock.Configuration;

public static class PathProvider
{
    private const string ConfigName = "settings.toml";
    private const string WeatherStateName = "weather.json";
    private const string DefaultLocation = "~/.config/alarm_clock";
    private static readonly string[] _locations = [DefaultLocation, "/etc/alarm_clock"];

    public static string GetConfigPath() => GetFilePath(ConfigName);
    public static string GetWeatherStatePath() => GetFilePath(WeatherStateName);
    
    private static string GetFilePath(string fileName)
    {
        var configPath = string.Empty;
        foreach (var dir in _locations)
        {
            var path = ExpandPath(Path.Combine(dir, fileName));

            if (File.Exists(path))
            {
                configPath = path;
                break;
            }
        }

        if (configPath == string.Empty)
            configPath = ExpandPath(Path.Combine(DefaultLocation, fileName));
        
        Directory.CreateDirectory(Path.GetDirectoryName(configPath)!);
        
        return ExpandPath(Path.Combine(DefaultLocation, fileName));
    }

    private static string ExpandPath(string path)
    {
        return !path.Contains('~')
            ? path
            : path.Replace("~", Environment.GetFolderPath(Environment.SpecialFolder.UserProfile));
    }
}