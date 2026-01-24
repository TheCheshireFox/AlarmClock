namespace AlarmClock.Shared.Extensions;

public static class ObjectExtension
{
    public static void WhenNotNull<T>(this T? obj, Action<T> action) where T : class
    {
        if (obj == null)
            return;
        
        action(obj);
    }
}