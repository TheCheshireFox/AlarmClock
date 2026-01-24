namespace AlarmClock.Buzzer;

public interface IAlarmBuzzer
{
    Task PlayAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}