using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Display.BacklightController;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public class MainWindowViewModel : ReactiveObject
{
    private readonly INavigationHost _navigationHost;
    private readonly IBacklightController _backlightController;

    public NavBarViewModel NavBar { get; }
    public StatusViewModel Status { get; }
    public DisplayAreaViewModel DisplayArea { get; }

    public MainWindowViewModel(INavigationHost navigationHost, IBacklightController backlightController, NavBarViewModel navBar, StatusViewModel status, DisplayAreaViewModel displayArea)
    {
        _navigationHost = navigationHost;
        _backlightController = backlightController;
        
        NavBar = navBar;
        Status = status;
        DisplayArea = displayArea;
    }

    public async Task DisplayOnAsync(CancellationToken cancellationToken)
    {
        await _backlightController.EnableAsync(true, cancellationToken);
    }

    public void ResetPage()
    {
        _navigationHost.NavigateTo<ClockViewModel>().Subscribe();
    }
}