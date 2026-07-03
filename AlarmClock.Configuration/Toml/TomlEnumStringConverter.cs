using Tomlyn.Serialization;

namespace AlarmClock.Configuration.Toml;

public class TomlEnumStringConverter : TomlConverter
{
    public override bool CanConvert(Type typeToConvert) => typeToConvert.IsEnum;

    public override object? Read(TomlReader reader, Type typeToConvert)
    {
        if (!typeToConvert.IsEnum)
            throw new InvalidOperationException($"Type '{typeToConvert.FullName}' is not an enum.");
        
        if (reader.TokenType != TomlTokenType.String)
            throw reader.CreateException($"Expected {TomlTokenType.String} token but was {reader.TokenType}.");

        var str = reader.GetString();
        
        try
        {
            var obj = Enum.Parse(typeToConvert, str, true);
            reader.Read();
            return obj;
        }
        catch (ArgumentException)
        {
            throw reader.CreateException($"Invalid enum name `{str}` for type '{typeToConvert.FullName}'.");
        }
    }

    public override void Write(TomlWriter writer, object? value)
    {
        if (value?.GetType().IsEnum is not true)
            throw new InvalidOperationException($"Type '{value?.GetType().FullName}' is not an enum.");
        
        writer.WriteStringValue(value.ToString() ?? throw new InvalidOperationException("String representation is null"));
    }
}