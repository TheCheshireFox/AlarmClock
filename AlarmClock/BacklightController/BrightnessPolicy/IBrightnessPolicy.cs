using System.Threading;
using System.Threading.Tasks;

namespace AlarmClock.BacklightController.BrightnessPolicy;

public interface IBrightnessPolicy
{
    bool IsActive { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
}