using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace AlarmClock.Configuration;

public class SnakeCaseJsonDataLoader
{
    public static IDictionary<string, string?> Parse(Stream jsonStream, StringComparer comparer)
    {
        var data = new Dictionary<string, string?>(comparer);
        var options = new JsonDocumentOptions
        {
            AllowTrailingCommas = true,
            CommentHandling = JsonCommentHandling.Skip
        };

        using var doc = JsonDocument.Parse(jsonStream, options);
        VisitElement(doc.RootElement, parentPath: null, sink: data);
        return data;
    }
    
    private static void VisitElement(JsonElement element, string? parentPath, IDictionary<string, string?> sink)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    var normalized = NormalizeSnakeCase(prop.Name);
                    var key = Combine(parentPath, normalized);
                    VisitElement(prop.Value, key, sink);
                }
                break;

            case JsonValueKind.Array:
                var i = 0;
                foreach (var item in element.EnumerateArray())
                {
                    var key = Combine(parentPath, i.ToString());
                    VisitElement(item, key, sink);
                    i++;
                }
                break;

            case JsonValueKind.String:
                sink[parentPath!] = element.GetString();
                break;

            case JsonValueKind.Number:
                sink[parentPath!] = element.GetRawText();
                break;

            case JsonValueKind.True:
            case JsonValueKind.False:
                sink[parentPath!] = element.GetBoolean().ToString();
                break;

            case JsonValueKind.Null:
                sink[parentPath!] = null;
                break;
            
            case JsonValueKind.Undefined:
            default:
                // ignore undefined
                break;
        }
    }

    private static string NormalizeSnakeCase(string name)
    {
        if (string.IsNullOrEmpty(name))
            return name;

        var sb = new StringBuilder();
        var upperNext = true;

        foreach (var c in name)
        {
            if (c == '_')
            {
                upperNext = true;
                continue;
            }

            sb.Append(upperNext ? char.ToUpperInvariant(c) : c);
            upperNext = false;
        }

        return sb.ToString();
    }
    
    private static string Combine(string? parent, string child) =>
        parent is null ? child : ConfigurationPath.Combine(parent, child);
}