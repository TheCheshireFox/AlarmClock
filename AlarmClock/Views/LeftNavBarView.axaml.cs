using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class LeftNavBarView : ReactiveUserControl<NavBarViewModel>
{
    public LeftNavBarView()
    {
        InitializeComponent();
    }
}