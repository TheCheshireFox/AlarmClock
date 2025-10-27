using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AlarmClock.Components;

public partial class LeftNavBar : UserControl
{
    public event EventHandler<MenuElement>? MenuSelected;
    
    public LeftNavBar()
    {
        InitializeComponent();
    }

    private void Clock_OnTapped(object? sender, TappedEventArgs e) => MenuSelected?.Invoke(this, MenuElement.Clock);

    private void Alarm_OnTapped(object? sender, TappedEventArgs e) => MenuSelected?.Invoke(this, MenuElement.Alarm);

    private void DisplayOff_OnTapped(object? sender, TappedEventArgs e) => MenuSelected?.Invoke(this, MenuElement.DisplayOff);

    private void Settings_OnTapped(object? sender, TappedEventArgs e) => MenuSelected?.Invoke(this, MenuElement.Settings);
}