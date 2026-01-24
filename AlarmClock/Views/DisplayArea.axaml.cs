using AlarmClock.ViewModels;
using ReactiveUI.Avalonia;

namespace AlarmClock.Views;

public partial class DisplayAreaView : ReactiveUserControl<DisplayAreaViewModel>
{ 
    public DisplayAreaView()
    {
        InitializeComponent();
    }
}