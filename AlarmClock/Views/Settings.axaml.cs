using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class SettingsView : ReactiveUserControl<SettingsViewModel>
{
    public SettingsView()
    {
        InitializeComponent();
    }
}