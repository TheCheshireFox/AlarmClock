using System;
using Microsoft.Extensions.Logging;

namespace AlarmClock.DisplayController;

public class DummyDisplayController : IDisplayController
{
    private readonly ILogger<DummyDisplayController> _logger;

    private bool _on = true;

    public DummyDisplayController(ILogger<DummyDisplayController> logger)
    {
        _logger = logger;
    }

    public void EnableBlanking(TimeSpan timeout) => _logger.LogDebug("EnableBlanking: {Timeout}", timeout);
    public void DisableBlanking() => _logger.LogDebug("DisableBlanking");
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