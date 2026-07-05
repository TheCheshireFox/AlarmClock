using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AlarmClock.Utility;

public sealed class MouseScrollGestureEmulator(Control control) : IDisposable
{
    private int _id;
    private bool _scrolling;
    private Point _lastPos;
    private double _velocity;
    private int _velocityCount;
    private DateTime _lastUpdate;

    public void Initialize()
    {
        control.AddHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        control.AddHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        control.AddHandler(InputElement.PointerMovedEvent, OnPointerMoved);
    }
    
    private void OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _scrolling = true;
        _id += 1;
        _lastPos = e.GetPosition(null);
        _lastUpdate = DateTime.Now;
        _velocity = 0;
        _velocityCount = 0;
    }

    private void OnPointerReleased(object? sender, PointerReleasedEventArgs e)
    {
        _scrolling = false;
        control.RaiseEvent(new ScrollGestureEndedEventArgs(_id));
    }

    private void OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (!_scrolling)
            return;
        
        var lastPos = e.GetPosition(null);
        var delta = lastPos - _lastPos;
        var lastUpdate = _lastUpdate;
        _lastPos = lastPos;
        _lastUpdate = DateTime.Now;

        if (_velocityCount == 0)
        {
            _velocity = delta.Y / (_lastUpdate - lastUpdate).TotalSeconds;
            _velocityCount = 1;
        }
        else
        {
            _velocity = (_velocity * _velocityCount + delta.Y / (_lastUpdate - lastUpdate).TotalSeconds) / (_velocityCount + 1);
            _velocityCount++;
        }
        
        control.RaiseEvent(new ScrollGestureEventArgs(_id, delta));
    }

    public void Dispose()
    {
        control.RemoveHandler(InputElement.PointerPressedEvent, OnPointerPressed);
        control.RemoveHandler(InputElement.PointerReleasedEvent, OnPointerReleased);
        control.RemoveHandler(InputElement.PointerMovedEvent, OnPointerMoved);
    }
}