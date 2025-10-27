using System;

namespace AlarmClock.Configuration;

[AttributeUsage(AttributeTargets.Class)]
public sealed class ConfigurationPathAttribute(string path) : Attribute
{
    public string Path { get; } = path;
}