namespace AlarmClock.Radio;

public interface IRadioListProvider
{
    IReadOnlyDictionary<string, string> Get();
}