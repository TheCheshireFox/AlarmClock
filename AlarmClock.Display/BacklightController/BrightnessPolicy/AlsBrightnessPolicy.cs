namespace AlarmClock.Display.BacklightController.BrightnessPolicy;

public class AlsBrightnessPolicy : IBrightnessPolicy
{
    public bool IsActive { get; } = false;

    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}