using AlarmClock.Gpio;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Display.DisplayController;

public interface IPwmDisplayControllerConfig
{
    int Pin { get; }
    float Frequency { get; }
}

public sealed class PwmDisplayController : IDisplayController, IDisposable
{
    private readonly IPwmDisplayControllerConfig _config;
    private readonly ILGpio _lGpio;
    private readonly ILogger<PwmDisplayController> _logger;
    private readonly int _handle;
    
    private bool _on = true;
    private double _dim;

    public PwmDisplayController(IPwmDisplayControllerConfig config, ILGpio lGpio, ILogger<PwmDisplayController> logger)
    {
        _config = config;
        _lGpio = lGpio;
        _logger = logger;
        
        _handle = _lGpio.Open(0);
        if (_handle < 0)
            throw new Exception("Failed to open GPIO pwm device");

        _ = _lGpio.TxPwm(_handle, _config.Pin, 0, 0, 0, 0);
        OnRaw(true);
    }

    public bool On(bool value)
    {
        if (value && _on && _dim == 0)
            return false;
        if (!value && !_on)
            return false;

        OnRaw(_on = value);
        _dim = 0;
        return true;
    }

    public bool Dim(double percent)
    {
        switch (percent)
        {
            case <= 0:
                return On(false);
            case >= 1:
                return On(true);
            default:
                Pwm((_dim = percent) * 100);
                return true;
        }
    }

    private void OnRaw(bool value)
    {
        _ = _lGpio.TxPwm(_handle, _config.Pin, 0, 0, 0, 0);
        
        var rc = _lGpio.ClaimOutput(_handle, 0, _config.Pin, /*initial*/ 0);
        if (rc < 0)
            throw new Exception($"GpioClaimOutput failed: {rc}");

        var level = value ? 1 : 0;
        rc = _lGpio.Write(_handle, _config.Pin, level);
        if (rc < 0)
            throw new Exception($"GpioWrite failed: {rc}");
        
        _logger.LogInformation("PWM set to {Value} with direct GPIO", level);
    }
    
    private void Pwm(double dutyCycle)
    {
        var rc = _lGpio.TxPwm(_handle, _config.Pin, _config.Frequency, (float)dutyCycle, 0, 0);
        if (rc < 0)
            throw new Exception($"TxPwm failed: {rc}");
        
        _logger.LogInformation("PWM set to {DutyCycle} with PWM GPIO", dutyCycle);
    }
    
    public void Dispose()
    {
        _ = _lGpio.Close(_handle);
    }
}