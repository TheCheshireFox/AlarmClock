using System;
using System.Diagnostics;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using AlarmClock.AlarmBuzzer;
using AlarmClock.Audio;
using AlarmClock.Audio.AudioDevice;
using AlarmClock.Audio.AudioSink;
using AlarmClock.Process;
using Microsoft.Extensions.Logging;
using AudioPriority = AlarmClock.Audio.AudioDevice.AudioPriority;

namespace AlarmClock.Radio;

public interface IRadioPlayer
{
    Task PlayAsync(string name, CancellationToken cancellationToken);
    Task StopAsync(CancellationToken cancellationToken);
}

public class RadioPlayer : IRadioPlayer
{
    private readonly IRadioListProvider _radioListProvider;
    private readonly IAudioDevice _audioDevice;
    private readonly AudioPriority _priority;
    private readonly ILogger<RadioAlarmBuzzer> _logger;

    private (Mpg123RadioAudioSource Source, IAudioSession Session)? _session;
    
    public RadioPlayer(IRadioListProvider radioListProvider, IAudioDevice audioDevice, AudioPriority priority, ILogger<RadioAlarmBuzzer> logger)
    {
        _radioListProvider = radioListProvider;
        _audioDevice = audioDevice;
        _priority = priority;
        _logger = logger;
    }
    
    public async Task PlayAsync(string name, CancellationToken cancellationToken)
    {
        if (!_radioListProvider.Get().TryGetValue(name, out var radioUrl))
            throw new Exception($"Radio {name} not found");

        await StopAsync(cancellationToken);

        var source = new Mpg123RadioAudioSource(new Uri(radioUrl));
        var audioSession = await _audioDevice.OpenSessionAsync(source, _priority, cancellationToken);
        _session = (source, audioSession);
    }

    public async Task StopAsync(CancellationToken cancellationToken)
    {
        if (_session is not {} session)
            return;

        await session.Source.DisposeAsync();
        await session.Session.DisposeAsync();

        _session = null;
        
        _logger.LogInformation("Radio stopped");
    }

    private class Mpg123RadioAudioSource : IAudioSource, IAsyncDisposable
    {
        private readonly Uri _uri;
        private readonly CancellationTokenSource _cts = new();

        private string _format = string.Empty;
        private int _sampleRate;
        private ScopedProcess? _mpg123Process;
        
        public bool CanPause => false;

        public Mpg123RadioAudioSource(Uri uri)
        {
            _uri = uri;
        }

        public Task InitializeAsync(AudioFormat format, CancellationToken cancellationToken)
        {
            _format = format.Encoding switch
            {
                AudioEncoding.Signed => "s",
                AudioEncoding.Unsigned => "u",
                AudioEncoding.FloatingPoint => "f",
                _ => throw new ArgumentOutOfRangeException()
            };
            _format += format.BitsPerSample.ToString();
            _sampleRate = format.SampleRate;
            
            StartMpg123Process();
            
            return Task.CompletedTask;
        }

        public async Task<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            if (_mpg123Process == null)
                return 0;
            
            return await _mpg123Process.StandardOutput.ReadAsync(buffer, cancellationToken);
        }

        public async Task PauseAsync(CancellationToken cancellationToken)
        {
            if (_mpg123Process == null)
                return;
            
            await _mpg123Process.TerminateAsync(5000, KillSignal.SIGTERM, cancellationToken);
            await _mpg123Process.DisposeAsync();
            _mpg123Process = null;
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            if (_mpg123Process is { Process.HasExited: false })
                return Task.CompletedTask;

            StartMpg123Process();
            
            return Task.CompletedTask;
        }

        private void StartMpg123Process()
        {
            _mpg123Process = new ScopedProcess(new ProcessStartInfo("mpg123")
            {
                ArgumentList = { "-q", "-s", "-r", _sampleRate.ToString(), "-e", _format, _uri.AbsoluteUri },
                RedirectStandardOutput = true
            }, cancellationToken: _cts.Token);
        }
        
        public async ValueTask DisposeAsync()
        {
            await _cts.CancelAsync();
            _cts.Dispose();

            if (_mpg123Process != null)
            {
                await _mpg123Process.DisposeAsync();
                _mpg123Process = null;
            }
        }
    }
}