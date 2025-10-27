using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;

namespace AlarmClock;

[Flags]
public enum Status
{
    RadioOn,
    RadioOff,
    AlarmOn,
    AlarmOff
}

public record StatusEvent(Status Status);

public interface IStatusBus
{
    StatusEvent? Last { get; }
    Task PublishAsync(StatusEvent evt, CancellationToken cancellationToken);
    IAsyncEnumerable<StatusEvent> SubscribeAsync(CancellationToken cancellationToken);
}

public class StatusBus : IStatusBus
{
    private readonly Channel<StatusEvent> _channel = Channel.CreateBounded<StatusEvent>(new BoundedChannelOptions(128)
    {
        FullMode = BoundedChannelFullMode.Wait,
        SingleReader = false,
        SingleWriter = false
    });
    
    private volatile StatusEvent? _last;
    
    public StatusEvent? Last => _last;

    public async Task PublishAsync(StatusEvent evt, CancellationToken cancellationToken)
    {
        await _channel.Writer.WriteAsync(_last = evt, cancellationToken);
    }

    public async IAsyncEnumerable<StatusEvent> SubscribeAsync([EnumeratorCancellation] CancellationToken cancellationToken)
    {
        if (_last is not null)
            yield return _last;

        await foreach (var evt in _channel.Reader.ReadAllAsync(cancellationToken))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return evt;
        }
    }
}