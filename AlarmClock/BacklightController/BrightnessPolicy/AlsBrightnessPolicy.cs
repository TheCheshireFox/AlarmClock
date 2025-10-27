using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Configuration;
using Microsoft.Extensions.Options;

namespace AlarmClock.BacklightController.BrightnessPolicy;

// TODO
public class AlsBrightnessPolicy : IBrightnessPolicy
{
    public bool IsActive { get; } = false;

    public AlsBrightnessPolicy(IOptionsMonitor<BacklightControlConfiguration> options)
    {
        
    }
    
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}