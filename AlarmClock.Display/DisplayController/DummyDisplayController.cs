using Microsoft.Extensions.Logging;

namespace AlarmClock.Display.DisplayController;

public class DummyDisplayController(ILogger<DummyDisplayController> logger) : IDisplayController
{
    private bool _on = true;

    public bool On(bool value)
    {
        logger.LogDebug("On: {Value}, old: {OldValue}", value, _on);
        if (_on == value)
            return false;
        
        _on = value;
        return true;
    }

    public bool Dim(double percent)
    {
        logger.LogDebug("Dim: {Value}", percent);
        return false;
    }
}