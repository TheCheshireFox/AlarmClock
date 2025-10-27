using System;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.AlarmBuzzer;
using AlarmClock.Announcer;
using AlarmClock.Configuration;
using AlarmClock.DependencyInjection;
using AlarmClock.Utility;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AlarmClock;

public record AlarmSettings(bool Enabled, TimeSpan Time);

public enum AlarmState
{
    Started,
    Stopped,
    WentOff
}

public interface IAlarmService
{
    event Action<TimeSpan>? Ticked;
    event Action<AlarmState>? Changed;
    
    AlarmState State { get; }
    
    Task InitializeAsync(CancellationToken cancellationToken);
    Task StartAsync(int hours, int minutes, CancellationToken cancellationToken);
    Task RestartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<AlarmSettings> GetAlarmAsync(CancellationToken cancellationToken);
}

public class AlarmService : IAlarmService
{
    private readonly IOptionsMonitor<AlarmConfiguration> _options;
    private readonly IJsonConfigManager _configurationManager;
    private readonly IKeyedOptionServiceProvider<IAnnouncer> _announcerProvider;
    private readonly ILogger<AlarmService> _logger;
    private readonly IKeyedOptionServiceProvider<IAlarmBuzzer> _alarmBuzzerProvider;
    private CancellationTokenSource _cts = new();
    private Task _alarmTask = Task.CompletedTask;
    private IAlarmBuzzer? _alarmBuzzer;

    public event Action<TimeSpan>? Ticked;
    public event Action<AlarmState>? Changed;
    
    public AlarmState State { get; private set; }
    
    public AlarmService(IOptionsMonitor<AlarmConfiguration> options,
        IJsonConfigManager configurationManager,
        IKeyedOptionServiceProvider<IAlarmBuzzer> alarmBuzzerProvider,
        IKeyedOptionServiceProvider<IAnnouncer> announcerProvider,
        ILogger<AlarmService> logger)
    {
        _options = options;
        _configurationManager = configurationManager;
        _alarmBuzzerProvider = alarmBuzzerProvider;
        _announcerProvider = announcerProvider;
        _logger = logger;
    }
    
    public async Task InitializeAsync(CancellationToken cancellationToken)
    {
        var alarm = _options.CurrentValue;
        
        if (!alarm.Enabled)
            return;

        await AnnouncerSayAsync($"Alarm set to {alarm.Time.Hour} hours, {alarm.Time.Minute} minutes", cancellationToken);
        
        await StartAsync(alarm.Time, cancellationToken);
    }
    
    public async Task StartAsync(int hours, int minutes, CancellationToken cancellationToken)
    {
        var target = DateTime.Now.Date.AddHours(hours).AddMinutes(minutes);
        
        await AnnouncerSayAsync($"Alarm set to {hours} hours, {minutes} minutes", cancellationToken);
        
        await StartAsync(target, cancellationToken);
    }

    public async Task RestartAsync(CancellationToken cancellationToken)
    {
        var alarm = _options.CurrentValue;

        await StartAsync(alarm.Time, cancellationToken);
        
        await AnnouncerSayAsync($"Alarm reset to {alarm.Time.Hour} hours, {alarm.Time.Minute} minutes", cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        var alarm = _options.CurrentValue;
        alarm.Enabled = false;
        
        _configurationManager.Update(alarm);

        await _cts.CancelAsync();
        await _alarmTask;
        
        if (_alarmBuzzer != null)
            await _alarmBuzzer.StopAsync(cancellationToken);
        
        _logger.LogInformation("Alarm stopped");
        await AnnouncerSayAsync("Alarm stopped", cancellationToken);
        
        ChangeState(AlarmState.Stopped);
    }

    public Task<AlarmSettings> GetAlarmAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();
        
        var alarm = _options.CurrentValue;
        return Task.FromResult(new AlarmSettings(alarm.Enabled, alarm.Time.TimeOfDay));
    }

    private async Task StartAsync(DateTime target, CancellationToken cancellationToken)
    {
        target = CalculateTarget(target);

        var alarm = _options.CurrentValue;
        alarm.Enabled = true;
        alarm.Time = target;
        
        _configurationManager.Update(alarm);

        await _cts.CancelAsync();
        await _alarmTask;
        
        if (_alarmBuzzer != null)
            await _alarmBuzzer.StopAsync(cancellationToken);

        _logger.LogInformation("Alarm set to {Time}", target);
        
        _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        _alarmTask = Task.Run(() => AlarmTickAsync(target, _cts.Token), _cts.Token);
        
        ChangeState(AlarmState.Started);
    }
    
    private async Task AlarmTickAsync(DateTime target, CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await Task.Delay(500, cancellationToken);
                
                var diff = target - DateTime.Now;
                if (diff < TimeSpan.Zero)
                    diff = TimeSpan.Zero;
                
                Ticked?.Invoke(diff);
                
                if (diff != TimeSpan.Zero)
                    continue;

                ChangeState(AlarmState.WentOff);
                
                _logger.LogInformation("Alarm went off");
                
                if (_alarmBuzzer != null)
                    await _alarmBuzzer.StopAsync(cancellationToken);

                _alarmBuzzer = _alarmBuzzerProvider.Get();
                await _alarmBuzzer.PlayAsync(cancellationToken);
                    
                break;
            }
            catch (OperationCanceledException)
            {
                return;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Alarm went off failed");
            }
        }
    }

    private void ChangeState(AlarmState state)
    {
        try
        {
            State = state;
            Changed?.Invoke(State);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while raising Changed event");
        }
    }
    
    private async Task AnnouncerSayAsync(string text, CancellationToken cancellationToken)
        => await _announcerProvider.Get().SayAsync(text, cancellationToken);
    
    private static DateTime CalculateTarget(DateTime target)
    {
        var offset = target - DateTime.Now;
        var daysOffset = offset < TimeSpan.Zero
            ? Math.Ceiling(-offset.TotalDays)
            : 0;

        return target.AddDays(daysOffset);
    }
}