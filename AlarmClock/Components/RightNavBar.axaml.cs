using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AlarmClock.Components;

public partial class RightNavBar : UserControl
{
    public event EventHandler<MenuElement>? MenuSelected;
    
    public RightNavBar()
    {
        InitializeComponent();
    }

    private void RadioButton_OnTapped(object? sender, TappedEventArgs e) => MenuSelected?.Invoke(this, MenuElement.Radio);
}