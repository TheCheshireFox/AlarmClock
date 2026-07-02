using AlarmClock.ViewModels;
using Avalonia.Interactivity;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class WiFiSettingsView : ReactiveUserControl<WiFiSettingsViewModel>
{
    public WiFiSettingsView()
    {
        InitializeComponent();
    }

    private void OnPasswordGotFocus(object? sender, RoutedEventArgs e)
    {
        if (ViewModel is not null)
        {
            ViewModel.IsKeyboardVisible = true;
        }
    }
}
