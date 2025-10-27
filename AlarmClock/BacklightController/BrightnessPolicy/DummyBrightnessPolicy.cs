using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.BacklightController.BrightnessPolicy;

public class DummyBrightnessPolicy : IBrightnessPolicy
{
    public bool IsActive => false;
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}