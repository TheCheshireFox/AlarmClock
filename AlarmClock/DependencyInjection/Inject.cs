using System;

namespace AlarmClock.DependencyInjection;

[AttributeUsage(AttributeTargets.Property)]
public sealed class InjectAttribute : Attribute;