using Microsoft.Extensions.Logging;

namespace AlarmClock.Display.DisplayController;

public class DummyDisplayController : IDisplayController
{
    private readonly ILogger<DummyDisplayController> _logger;

    private bool _on = true;

    public DummyDisplayController(ILogger<DummyDisplayController> logger)
    {
        _logger = logger;
    }

    public bool On(bool value)
    {
        _logger.LogDebug("On: {Value}, old: {OldValue}", value, _on);
        if (_on == value)
            return false;
        
        _on = value;
        return true;
    }

    public bool Dim(double percent)
    {
        _logger.LogDebug("Dim: {Value}", percent);
        return false;
    }
}