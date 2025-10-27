using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;

namespace AlarmClock.Components;

public class MouseScrollGestureEmulator
{
    private readonly Control _control;

    private int _id;
    private bool _scrolling;
    private Point _lastPos;
    private double _velocity;
    private int _velocityCount;
    private DateTime _lastUpdate;

    public MouseScrollGestureEmulator(Control control)
    {
        _control = control;
    }
    
    public void OnPointerPressed(PointerPressedEventArgs e)
    {
        _scrolling = true;
        _id += 1;
        _lastPos = e.GetPosition(null);
        _lastUpdate = DateTime.Now;
        _velocity = 0;
        _velocityCount = 0;
    }

    public void OnPointerReleased(PointerReleasedEventArgs e)
    {
        _scrolling = false;
        _control.RaiseEvent(new ScrollGestureEndedEventArgs(_id));
    }

    public void OnPointerMoved(PointerEventArgs e)
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
        
        _control.RaiseEvent(new ScrollGestureEventArgs(_id, delta));
    }
}

public partial class NumberPicker : UserControl
{
    public static readonly StyledProperty<double> ContentHeightProperty = AvaloniaProperty.Register<NumberPicker, double>(nameof(ContentHeight), 64);
    public static readonly StyledProperty<double> ControlHeightProperty = AvaloniaProperty.Register<NumberPicker, double>(nameof(ControlHeight), 128);
    public static readonly StyledProperty<int> MinProperty = AvaloniaProperty.Register<NumberPicker, int>(nameof(Min));
    public static readonly StyledProperty<int> MaxProperty = AvaloniaProperty.Register<NumberPicker, int>(nameof(Max), 100);
    public static readonly StyledProperty<int> SelectedItemProperty = AvaloniaProperty.Register<NumberPicker, int>(nameof(SelectedItem));
    public static readonly StyledProperty<bool> ReverseProperty = AvaloniaProperty.Register<NumberPicker, bool>(nameof(SelectedItem));
    public static readonly StyledProperty<string> SelectedItemTextProperty = AvaloniaProperty.Register<NumberPicker, string>(nameof(SelectedItemText), "00");

    private readonly MouseScrollGestureEmulator _gestureEmulator;
    private double _scrollDistance;
    
    public double ContentHeight
    {
        get => GetValue(ContentHeightProperty);
        set => SetValue(ContentHeightProperty, value);
    }
    
    public double ControlHeight
    {
        get => GetValue(ControlHeightProperty);
        set => SetValue(ControlHeightProperty, value);
    }
    
    public int Min
    {
        get => GetValue(MinProperty);
        set => SetValue(MinProperty, value);
    }
    
    public int Max
    {
        get => GetValue(MaxProperty);
        set => SetValue(MaxProperty, value);
    }
    
    public int SelectedItem
    {
        get => GetValue(SelectedItemProperty);
        set
        {
            SetValue(SelectedItemProperty, value);
            SetSelectedItemText();
        }
    }

    private string SelectedItemText
    {
        get => GetValue(SelectedItemTextProperty);
        set => SetValue(SelectedItemTextProperty, value);
    }
    
    public bool Reverse
    {
        get => GetValue(ReverseProperty);
        set => SetValue(ReverseProperty, value);
    }
    
    public NumberPicker()
    {
        InitializeComponent();

        _gestureEmulator = new MouseScrollGestureEmulator(this);
        AddHandler(Gestures.ScrollGestureEvent, ScrollGesture);
        AddHandler(Gestures.ScrollGestureEndedEvent, ScrollGestureEnded);
    }

    private void ScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        _scrollDistance += e.Delta.Y;
        var incr = _scrollDistance > 0;
        _scrollDistance = Math.Abs(_scrollDistance);

        while (_scrollDistance >= ContentHeight)
        {
            SelectedItem = incr ? Increment() : Decrement();
            SetSelectedItemText();
            _scrollDistance -= ContentHeight;
        }

        if (!incr)
            _scrollDistance *= -1;
        
        e.Handled = true;
    }

    private void ScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        e.Handled = true;
    }

    protected override void OnPointerPressed(PointerPressedEventArgs e) => _gestureEmulator.OnPointerPressed(e);
    protected override void OnPointerReleased(PointerReleasedEventArgs e) => _gestureEmulator.OnPointerReleased(e);
    protected override void OnPointerMoved(PointerEventArgs e) => _gestureEmulator.OnPointerMoved(e);

    private int Increment()
    {
        var selectedItem = SelectedItem + 1;
        if (selectedItem > Max)
            selectedItem = Min;
        return selectedItem;
    }
    
    private int Decrement()
    {
        var selectedItem = SelectedItem - 1;
        if (selectedItem < Min)
            selectedItem = Max;
        return selectedItem;
    }

    private void SetSelectedItemText()
    {
        var number = Math.Max(Math.Abs(Min), Math.Abs(Max));
        var padding = number == 0 
            ? 1
            : 1 + (int)Math.Floor(Math.Log10(number));

        SelectedItemText = SelectedItem.ToString($"D{padding}");
    }

    private class PointerScrollGestureState
    {
        public int Id { get; set; }
        public bool Scrolling { get; set; }
        public Point LastPos { get; set; }
    }
}