using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia.Platform;

namespace AlarmClock.AlarmBuzzer;

public interface IAlarmListProvider
{
    IReadOnlyDictionary<string, Uri> Get();
}

public class AlarmListProvider : IAlarmListProvider
{
    private readonly Dictionary<string, Uri> _alarms;

    public AlarmListProvider()
    {
        var sounds = AssetLoader.GetAssets(new Uri("avares://AlarmClock/Assets/Sounds"), null);
        _alarms = sounds.ToDictionary(x => x.Segments[^1].Split('.')[0], x => x);
    }

    public IReadOnlyDictionary<string, Uri> Get() => _alarms;
}