namespace AlarmClock.Display.BacklightController.BrightnessPolicy;

public interface IBrightnessPolicy
{
    bool IsActive { get; }
    Task InitializeAsync(CancellationToken cancellationToken);
}