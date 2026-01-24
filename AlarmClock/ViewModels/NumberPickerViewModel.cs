using System;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class NumberPickerViewModel : ReactiveObject
{
    private int _selectedItem;
    private double _contentHeight = 64;
    private string _selectedItemText = "00";
    private double _scrollDistance;

    public int Min { get; set; }
    public int Max { get; set; } = 100;
    
    public double ContentHeight
    {
        get => _contentHeight;
        set => this.RaiseAndSetIfChanged(ref _contentHeight, value);
    }
    
    public int SelectedItem
    {
        get => _selectedItem;
        set
        {
            this.RaiseAndSetIfChanged(ref _selectedItem, value);
            SetSelectedItemText();
        }
    }

    public string SelectedItemText
    {
        get => _selectedItemText;
        set => this.RaiseAndSetIfChanged(ref _selectedItemText, value);
    }

    public void ScrollBy(double delta)
    {
        _scrollDistance += delta;
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
    }

    private int Increment()
    {
        var selectedItem = SelectedItem + 1;
        return selectedItem > Max ? Min : selectedItem;
    }

    private int Decrement()
    {
        var selectedItem = SelectedItem - 1;
        return selectedItem < Min ? Max : selectedItem;
    }

    private void SetSelectedItemText()
    {
        var number = Math.Max(Math.Abs(Min), Math.Abs(Max));
        var padding = number == 0 
            ? 1
            : 1 + (int)Math.Floor(Math.Log10(number));

        SelectedItemText = SelectedItem.ToString($"D{padding}");
    }
}