using System;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AlarmClock.Views;

public class WakeShield
{
    private readonly BehaviorSubject<Unit> _wake = new(Unit.Default);
    
    private bool _wakeWaitForRelease;
    private bool _active;
    private Interactive? _control;

    public IObservable<Unit> OnWake => _wake.AsObservable();
        
    public void Attach(Interactive control)
    {
        _control = control;
        _wakeWaitForRelease = false;
        _active = false;
            
        control.AddHandler(InputElement.PointerPressedEvent, (_, e) =>
        {
            if (!_active)
            {
                _wake.OnNext(Unit.Default);
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
                
            _wake.OnNext(Unit.Default);
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