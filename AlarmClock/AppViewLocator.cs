using System;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace AlarmClock;

public class AppViewLocator : IViewLocator
{
    private readonly IServiceProvider _serviceProvider = App.Services;

    public IViewFor? ResolveView<T>(T? viewModel, string? contract = null)
        => (IViewFor?)_serviceProvider.GetRequiredService(typeof(IViewFor<>).MakeGenericType(viewModel?.GetType() ?? typeof(T)));
}