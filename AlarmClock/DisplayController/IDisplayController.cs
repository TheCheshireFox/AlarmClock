using System;

namespace AlarmClock.DisplayController;

public interface IDisplayController
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="value"></param>
    /// <returns>True if the state was changed, false otherwise</returns>
    bool On(bool value);

    bool Dim(double percent);
}