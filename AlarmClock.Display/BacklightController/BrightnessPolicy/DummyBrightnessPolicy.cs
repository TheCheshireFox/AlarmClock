namespace AlarmClock.Display.BacklightController.BrightnessPolicy;

public class DummyBrightnessPolicy : IBrightnessPolicy
{
    public bool IsActive => false;
    public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
}