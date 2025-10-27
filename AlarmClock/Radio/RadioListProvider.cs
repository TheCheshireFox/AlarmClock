using System;
using System.Collections.Generic;
using System.Text.Json;
using Avalonia.Platform;

namespace AlarmClock.Radio;

public interface IRadioListProvider
{
    IReadOnlyDictionary<string, string> Get();
}

public class RadioListProvider : IRadioListProvider
{
    private const string AssetPath = "avares://AlarmClock/Assets/radio.json";
    private readonly Dictionary<string, string> _radios = [];

    public RadioListProvider()
    {
        var uri = new Uri(AssetPath);

        if (!AssetLoader.Exists(uri))
            return;
        
        using var json = AssetLoader.Open(uri);
        _radios = JsonSerializer.Deserialize<Dictionary<string, string>>(json) ?? throw new Exception("Failed to load radio list");
    }

    public IReadOnlyDictionary<string, string> Get() => _radios;
}