using System.Globalization;
using Microsoft.Extensions.Configuration;
using Tomlyn;
using Tomlyn.Model;

namespace AlarmClock.Configuration.Toml;

file sealed class TomlConfigurationFileParser
{
    private readonly IDictionary<string, string> _data =
        new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

    private readonly Stack<string> _paths = new();

    private TomlConfigurationFileParser()
    {
    }

    public static IDictionary<string, string> Parse(Stream input, TomlSerializerOptions? options)
    {
        return new TomlConfigurationFileParser().ParseStream(input, options);
    }

    private IDictionary<string, string> ParseStream(Stream input, TomlSerializerOptions? options)
    {
        using var streamReader = new StreamReader(input);
        VisitObject(TomlSerializer.Deserialize<TomlTable>(streamReader.ReadToEnd(), options) ??
                    throw new FormatException("TOML deserialization returned null"));
        return _data;
    }

    private void VisitTable(TomlTable table)
    {
        foreach (var keyValuePair in table)
        {
            EnterContext(keyValuePair.Key);
            VisitObject(keyValuePair.Value);
            ExitContext();
        }
    }

    private void VisitTableArray(TomlTableArray tableArray)
    {
        for (var index = 0; index < tableArray.Count; ++index)
        {
            EnterContext(index.ToString());
            VisitTable(tableArray[index]);
            ExitContext();
        }
    }

    private void VisitArray(TomlArray array)
    {
        var num = 0;
        foreach (var obj in array)
        {
            EnterContext(num++.ToString());
            VisitObject(obj!);
            ExitContext();
        }
    }

    private void VisitObject(object obj)
    {
        switch (obj)
        {
            case TomlTable table:
                VisitTable(table);
                break;
            case TomlTableArray tableArray:
                VisitTableArray(tableArray);
                break;
            case TomlArray array:
                VisitArray(array);
                break;
            default:
                _data.Add(_paths.Peek(), Convert.ToString(obj, CultureInfo.InvariantCulture)!);
                break;
        }
    }

    private void EnterContext(string context)
    {
        _paths.Push(_paths.Count > 0 ? _paths.Peek() + ConfigurationPath.KeyDelimiter + context : context);
    }

    private void ExitContext() => _paths.Pop();
}

public class TomlWithOptionsConfigurationProvider(FileConfigurationSource source, TomlSerializerOptions? options)
    : FileConfigurationProvider(source)
{
    public override void Load(Stream stream)
    {
        try
        {
            Data = TomlConfigurationFileParser.Parse(stream, options)!;
        }
        catch (Exception ex)
        {
            throw new FormatException("TOML parse failed", ex);
        }
    }
}