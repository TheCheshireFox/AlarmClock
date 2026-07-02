using System;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace AlarmClock;

public class AppViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider = App.Services;

    public IViewFor<TViewModel>? ResolveView<TViewModel>(string? contract = null) where TViewModel : class
        => (IViewFor<TViewModel>?)_serviceProvider.GetRequiredService<IViewFor<TViewModel>>();

    public IViewFor? ResolveView(object? instance, string? contract = null)
        => (IViewFor?)_serviceProvider.GetRequiredService(typeof(IViewFor<>).MakeGenericType(instance?.GetType()!));
}