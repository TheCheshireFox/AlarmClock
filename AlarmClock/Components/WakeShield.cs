using System;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AlarmClock.Components;

public class WakeShield
{
    private bool _wakeWaitForRelease;
    private bool _active;
    private Interactive? _control;

    public event Action? Wake; 
        
    public void Attach(Interactive control)
    {
        _control = control;
        _wakeWaitForRelease = false;
        _active = false;
            
        control.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (!_active)
            {
                Wake?.Invoke();
                return;
            }
                
            e.Handled = true;
            _wakeWaitForRelease = true;
        }, RoutingStrategies.Tunnel);
            
        control.AddHandler(InputElement.PointerReleasedEvent, (_, e) =>
        {
            if (!_active || !_wakeWaitForRelease)
                return;
                
            e.Handled = true;
            _wakeWaitForRelease = false;
            _active = false;
                
            Wake?.Invoke();
        }, RoutingStrategies.Tunnel);
    }

    public void Activate()
    {
        if (_control == null)
            return;

        _active = true;
        _wakeWaitForRelease = false;
    }
}