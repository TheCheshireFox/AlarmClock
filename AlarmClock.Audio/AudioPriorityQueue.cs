using AlarmClock.Audio.AudioDevice;

namespace AlarmClock.Audio;

internal class AudioPriorityComparer : IComparer<AudioPriority>
{
    public int Compare(AudioPriority x, AudioPriority y) => y - x;
}

internal class AudioPriorityQueue<T> where T: class
{
    private readonly PriorityQueue<T, AudioPriority> _queue = new(new AudioPriorityComparer());
    private T? _exclusiveRequest;

    public T? GetActive()
    {
        if (_exclusiveRequest != null)
            return _exclusiveRequest;

        return _queue.TryPeek(out var value, out _) ? value : null;
    }
    
    public bool TryEnqueue(T value, AudioPriority priority)
    {
        if (priority != AudioPriority.Exclusive)
        {
            if (_queue.UnorderedItems.Any(x => x.Element == value && x.Priority == priority))
                return false;
            
            _queue.Enqueue(value, priority);
            return true;
        }
        
        if (_exclusiveRequest != null)
            throw new InvalidOperationException("Another exclusive audio session requested");
            
        _exclusiveRequest = value;
        return true;
    }

    public T? RemoveBy(Func<T, bool> predicate)
    {
        if (_exclusiveRequest != null && predicate(_exclusiveRequest))
        {
            var ret = _exclusiveRequest;
            _exclusiveRequest = null;
            return ret;
        }

        foreach (var (value, _) in _queue.UnorderedItems)
        {
            if (!predicate(value))
                continue;
            
            _queue.Remove(value, out _, out _);
            return value;
        }

        return null;
    }
}