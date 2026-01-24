using AlarmClock.Extensions;
using AlarmClock.Shared.Extensions;
using AlarmClock.Utility;
using AlarmClock.ViewModels;
using Avalonia;
using Avalonia.Input;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class NumberPickerView : ReactiveUserControl<NumberPickerViewModel>
{
    public static readonly StyledProperty<double> ContentHeightProperty = AvaloniaProperty.Register<NumberPickerView, double>(nameof(ContentHeight), 64);

    public double ContentHeight
    {
        get => ViewModel?.ContentHeight ?? GetValue(ContentHeightProperty);
        set
        {
            ViewModel.WhenNotNull(x => x.ContentHeight = value);
            SetValue(ContentHeightProperty, value);
        }
    }
    
    public NumberPickerView()
    {
        InitializeComponent();

        var gestureEmulator = new MouseScrollGestureEmulator(this);
        gestureEmulator.Initialize();
        
        AddHandler(Gestures.ScrollGestureEvent, ScrollGesture);
        AddHandler(Gestures.ScrollGestureEndedEvent, ScrollGestureEnded);

        this.WhenActivated(disposables =>
        {
            disposables.Add(gestureEmulator);
            
            if (ViewModel == null)
                return;

            ViewModel.ContentHeight = GetValue(ContentHeightProperty);
        });
    }

    private void ScrollGesture(object? sender, ScrollGestureEventArgs e)
    {
        ViewModel?.ScrollBy(e.Delta.Y);
        e.Handled = true;
    }

    private void ScrollGestureEnded(object? sender, ScrollGestureEndedEventArgs e)
    {
        e.Handled = true;
    }
}