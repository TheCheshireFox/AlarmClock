using System;
using System.Runtime.InteropServices;
using AlarmClock.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock.DisplayController;

file static class Lgpio
{
    private const string Lib = "lgpio";
    
    [DllImport(Lib, EntryPoint = "lgGpiochipOpen", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpiochipOpen(int gpioDev);
    
    [DllImport(Lib, EntryPoint = "lgGpiochipClose", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpiochipClose(int handle);
    
    [DllImport(Lib, EntryPoint = "lgTxPwm", CallingConvention = CallingConvention.Cdecl)]
    public static extern int TxPwm(int handle, int gpio, float pwmFrequency, float pwmDutyCycle, int pwmOffset, int pwmCycles);
    
    [DllImport(Lib, EntryPoint = "lgGpioClaimOutput", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpioClaimOutput(int handle, int lFlags, int line, int level);
    
    [DllImport(Lib, EntryPoint = "lgGpioWrite", CallingConvention = CallingConvention.Cdecl)]
    public static extern int GpioWrite(int handle, int line, int level);
}


public sealed class PwmDisplayController : IDisplayController, IDisposable
{
    private readonly IOptionsMonitor<DisplayControllerConfiguration> _options;
    private readonly ILogger<PwmDisplayController> _logger;
    private readonly int _handle;
    
    private bool _on = true;
    private double _dim;

    public PwmDisplayController(IOptionsMonitor<DisplayControllerConfiguration> options, ILogger<PwmDisplayController> logger)
    {
        _options = options;
        _logger = logger;
        
        _handle = Lgpio.GpiochipOpen(0);
        if (_handle < 0)
            throw new Exception("Failed to open GPIO pwm device");

        _ = Lgpio.TxPwm(_handle, options.CurrentValue.Pwm.Pin, 0, 0, 0, 0);
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
        _ = Lgpio.TxPwm(_handle, _options.CurrentValue.Pwm.Pin, 0, 0, 0, 0);
        
        var rc = Lgpio.GpioClaimOutput(_handle, 0, _options.CurrentValue.Pwm.Pin, /*initial*/ 0);
        if (rc < 0)
            throw new Exception($"GpioClaimOutput failed: {rc}");

        var level = value ? 1 : 0;
        rc = Lgpio.GpioWrite(_handle, _options.CurrentValue.Pwm.Pin, level);
        if (rc < 0)
            throw new Exception($"GpioWrite failed: {rc}");
        
        _logger.LogInformation("PWM set to {Value} with direct GPIO", level);
    }
    
    private void Pwm(double dutyCycle)
    {
        var rc = Lgpio.TxPwm(_handle, _options.CurrentValue.Pwm.Pin, _options.CurrentValue.Pwm.Frequency, (float)dutyCycle, 0, 0);
        if (rc < 0)
            throw new Exception($"TxPwm failed: {rc}");
        
        _logger.LogInformation("PWM set to {DutyCycle} with PWM GPIO", dutyCycle);
    }
    
    public void Dispose()
    {
        _ = Lgpio.GpiochipClose(_handle);
    }
}