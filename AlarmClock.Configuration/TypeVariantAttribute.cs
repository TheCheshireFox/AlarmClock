namespace AlarmClock.Configuration;

[AttributeUsage(AttributeTargets.Property,  AllowMultiple = true)]
public sealed class TypeVariantAttribute(object variant) : Attribute
{
    public object Variant { get; } = variant;
}