using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class HeaderView : ReactiveUserControl<StatusViewModel>
{
    public HeaderView()
    {
        InitializeComponent();
    }
}