using System.Collections;

namespace AlarmClock.Shared.Extensions;

public static class EnumerableExtension
{
    public static bool Any(this IEnumerable enumerable)
    {
        var enumerator = enumerable.GetEnumerator();
        try
        {
            return enumerator.MoveNext();
        }
        finally
        {
            if (enumerator is IDisposable disposable)
                disposable.Dispose();
        }
    }
}