using AlarmClock.Gpio;
using Microsoft.Extensions.Logging;

namespace AlarmClock.Buzzer;

public interface IGpioBuzzerConfig
{
    int Pin { get; }
}

public sealed class GpioAlarmBuzzer : IAlarmBuzzer, IDisposable
{
    private readonly IGpioBuzzerConfig _config;
    private readonly ILGpio _lGpio;
    private readonly ILogger<GpioAlarmBuzzer> _logger;
    private readonly int _handle;
    
    private Task _beepTask = Task.CompletedTask;
    private CancellationTokenSource _cts = new();

    public GpioAlarmBuzzer(IGpioBuzzerConfig config, ILGpio lGpio, ILogger<GpioAlarmBuzzer> logger)
    {
        _config = config;
        _lGpio = lGpio;
        _logger = logger;

        _handle = _lGpio.Open(0);
        if (_handle < 0)
            throw new Exception($"Failed to open GPIO chip: {_handle}");

        var rc = _lGpio.ClaimOutput(_handle, 0, _config.Pin, 0);
        if (rc < 0)
            throw new Exception($"GpioClaimOutput failed for pin {_config.Pin}: {rc}");

        _logger.LogInformation("GPIO buzzer initialized on pin {Pin}", _config.Pin);
    }

    public async Task PlayAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        if (!_beepTask.IsCompleted)
            return;

        await ResetBeepAsync();
        _beepTask = BeepAsync(_cts.Token);

        _logger.LogInformation("GPIO buzzer started on pin {Pin}", _config.Pin);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        await ResetBeepAsync();
    }
    
    private async Task BeepAsync(CancellationToken cancellationToken)
    {
        try
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                GpioWrite(1);
                await Task.Delay(250, cancellationToken);

                GpioWrite(0);
                await Task.Delay(250, cancellationToken);

                GpioWrite(1);
                await Task.Delay(250, cancellationToken);

                GpioWrite(0);
                await Task.Delay(1250, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // NOP
        }
        finally
        {
            _ = _lGpio.Write(_handle, _config.Pin, 0);
        }
    }
    
    private async Task ResetBeepAsync()
    {
        await _cts.CancelAsync();
        try
        {
            await _beepTask;
        }
        finally
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }

    private void GpioWrite(int level)
    {
        var rc = _lGpio.Write(_handle, _config.Pin, level);
        if (rc < 0)
            throw new Exception($"GpioWrite HIGH failed for pin {_config.Pin}: {rc}");
    }
    
    public void Dispose()
    {
        _ = _lGpio.Close(_handle);
    }
}
