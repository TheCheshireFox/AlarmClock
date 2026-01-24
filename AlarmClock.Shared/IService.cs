namespace AlarmClock.Shared;

public interface IService<out T> where T : notnull
{
    T Get();
}