using System;
using Microsoft.Extensions.DependencyInjection;
using ReactiveUI;

namespace AlarmClock;

public interface IViewModelFactory
{
    T Create<T>() where T : IReactiveObject;
}

public class ViewModelFactory : IViewModelFactory
{
    private readonly IServiceProvider _serviceProvider = App.Services;

    public T Create<T>() where T : IReactiveObject => _serviceProvider.GetRequiredService<T>();
}