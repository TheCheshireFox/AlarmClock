using System.Threading;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;

namespace AlarmClock.Utility;

public static class ApplicationCancellation
{
    public static CancellationToken Token { get; }
    
    static ApplicationCancellation()
    {
        if (Application.Current?.ApplicationLifetime is not IClassicDesktopStyleApplicationLifetime lifetime)
        {
            Token = CancellationToken.None;
            return;
        }

        CancellationTokenSource cts = new();
        lifetime.Exit += (_, _) => cts.Cancel();
    }
}