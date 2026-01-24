using System;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Threading;
using ReactiveUI;

namespace AlarmClock.ViewModels;

public interface INavigationHost : IScreen
{
    IObservable<IRoutableViewModel> NavigateTo<T>() where T : IRoutableViewModel;
}

public class DisplayAreaViewModel : ReactiveObject, INavigationHost, IActivatableViewModel
{
    private readonly IViewModelFactory _viewModelFactory;

    public ViewModelActivator Activator { get; } = new();
    public RoutingState Router { get; } = new();

    public DisplayAreaViewModel(IViewModelFactory viewModelFactory, IAlarmService alarmService)
    {
        _viewModelFactory = viewModelFactory;

        this.WhenActivated(disposables =>
        {
            alarmService.StateChanged
                .ObserveOn(RxApp.MainThreadScheduler)
                .Subscribe(state =>
                {
                    if (state == AlarmState.WentOff)
                        NavigateTo<AlarmClockViewModel>();
                })
                .DisposeWith(disposables);
        });
    }
    
    public IObservable<IRoutableViewModel> NavigateTo<T>() where T : IRoutableViewModel
        => Router.Navigate.Execute(_viewModelFactory.Create<T>());
}