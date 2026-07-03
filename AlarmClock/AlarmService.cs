using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.Announcer;
using AlarmClock.Buzzer;
using AlarmClock.Configuration;
using AlarmClock.Extensions;
using AlarmClock.Shared;
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
    IObservable<TimeSpan> Ticked { get; }
    IObservable<AlarmState> StateChanged { get; }
    
    Task InitializeAsync(CancellationToken cancellationToken);
    Task StartAsync(int hours, int minutes, CancellationToken cancellationToken);
    Task RestartAsync(CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
    Task<AlarmSettings> GetAlarmAsync(CancellationToken cancellationToken);
}

public class AlarmService : IAlarmService
{
    private readonly BehaviorSubject<AlarmState> _state = new(AlarmState.Stopped);
    private readonly BehaviorSubject<TimeSpan> _tick = new(TimeSpan.Zero);

    private readonly IOptionsMonitor<AlarmConfiguration> _options;
    private readonly IConfigManager _configurationManager;
    private readonly IService<IAnnouncer> _announcerService;
    private readonly ILogger<AlarmService> _logger;
    private readonly IAlarmBuzzer _alarmBuzzer;
    private CancellationTokenSource _cts = new();
    private Task _alarmTask = Task.CompletedTask;

    public IObservable<TimeSpan> Ticked => _tick.AsObservable();
    public IObservable<AlarmState> StateChanged => _state.AsObservable();
    
    public AlarmState State { get; private set; }
    
    public AlarmService(IOptionsMonitor<AlarmConfiguration> options,
        IConfigManager configurationManager,
        IAlarmBuzzer alarmBuzzer,
        IService<IAnnouncer> announcerService,
        ILogger<AlarmService> logger)
    {
        _options = options;
        _configurationManager = configurationManager;
        _alarmBuzzer = alarmBuzzer;
        _announcerService = announcerService;
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
        _configurationManager.Update(_options, alarm => alarm.Enabled = false);

        await ResetAlarmTaskAsync();

        _cts = new CancellationTokenSource();

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

        _configurationManager.Update(_options, alarm =>
        {
            alarm.Enabled = true;
            alarm.Time = target;
        });

        await ResetAlarmTaskAsync();
        
        await _alarmBuzzer.StopAsync(cancellationToken);

        _logger.LogInformation("Alarm set to {Time}", target);

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
                
                _tick.OnNext(diff);
                
                if (diff != TimeSpan.Zero)
                    continue;

                ChangeState(AlarmState.WentOff);
                
                _logger.LogInformation("Alarm went off");
                
                await _alarmBuzzer.StopAsync(cancellationToken);
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
            _state.OnNext(State = state);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error while raising Changed event");
        }
    }
    
    private async Task ResetAlarmTaskAsync()
    {
        await _cts.CancelAsync();
        try
        {
            await _alarmTask;
        }
        finally
        {
            _cts.Dispose();
            _cts = new CancellationTokenSource();
        }
    }
    
    private async Task AnnouncerSayAsync(string text, CancellationToken cancellationToken)
        => await _announcerService.Get().SayAsync(text, cancellationToken);
    
    private static DateTime CalculateTarget(DateTime target)
    {
        var offset = target - DateTime.Now;
        var daysOffset = offset < TimeSpan.Zero
            ? Math.Ceiling(-offset.TotalDays)
            : 0;

        return target.AddDays(daysOffset);
    }
}