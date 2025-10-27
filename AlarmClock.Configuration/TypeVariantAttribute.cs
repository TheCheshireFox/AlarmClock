using System;

namespace AlarmClock.Configuration;

[AttributeUsage(AttributeTargets.Property,  AllowMultiple = true)]
public sealed class TypeVariantAttribute(string name) : Attribute
{
    public string Name { get; } = name;
}