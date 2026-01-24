using Microsoft.Extensions.Logging.Console;

namespace AlarmClock.Logging;

public class TemplateConsoleFormatterOptions : ConsoleFormatterOptions
{
    public string Template { get; init; } = TemplateConsoleFormatter.DefaultTemplate;
}