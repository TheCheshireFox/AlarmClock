using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class FooterView : ReactiveUserControl<StatusViewModel>
{
    public FooterView()
    {
        InitializeComponent();
    }
}