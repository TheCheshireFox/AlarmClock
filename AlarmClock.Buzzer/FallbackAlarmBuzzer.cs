using AlarmClock.Shared;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Buzzer;

public sealed class FallbackAlarmBuzzer(
    IService<IAlarmBuzzer> primaryBuzzer,
    IAlarmBuzzer fallbackBuzzer,
    ILogger<FallbackAlarmBuzzer> logger) : IAlarmBuzzer
{
    private bool _fallback;

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        try
        {
            _fallback = false;
            await primaryBuzzer.Get().PlayAsync(cancellationToken);
        }
        catch (OperationCanceledException)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Primary buzzer failed to start, falling back to fallback buzzer");

            _fallback = true;
            await fallbackBuzzer.PlayAsync(cancellationToken);
        }
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var buzzer = _fallback ? fallbackBuzzer : primaryBuzzer.Get();
        await buzzer.StopAsync(cancellationToken);
    }
}
