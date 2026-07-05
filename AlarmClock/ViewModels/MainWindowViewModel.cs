using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Display.BacklightController;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class MainWindowViewModel(
    INavigationHost navigationHost,
    IBacklightController backlightController,
    NavBarViewModel navBar,
    StatusViewModel status,
    DisplayAreaViewModel displayArea)
    : ReactiveObject
{
    public NavBarViewModel NavBar { get; } = navBar;
    public StatusViewModel Status { get; } = status;
    public DisplayAreaViewModel DisplayArea { get; } = displayArea;

    public async Task DisplayOnAsync(CancellationToken cancellationToken)
    {
        await backlightController.EnableAsync(true, cancellationToken);
    }

    public void ResetPage()
    {
        navigationHost.NavigateTo<ClockViewModel>().Subscribe();
    }
}