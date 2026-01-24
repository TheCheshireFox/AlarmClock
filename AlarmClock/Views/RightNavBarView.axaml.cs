using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class RightNavBarView : ReactiveUserControl<NavBarViewModel>
{
    public RightNavBarView()
    {
        InitializeComponent();
    }
}