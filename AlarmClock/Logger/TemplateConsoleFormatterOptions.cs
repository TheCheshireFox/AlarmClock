using Microsoft.Extensions.Logging.Console;

namespace AlarmClock.Logger;

public class TemplateConsoleFormatterOptions : ConsoleFormatterOptions
{
    public string Template { get; init; } = TemplateConsoleFormatter.DefaultTemplate;
}