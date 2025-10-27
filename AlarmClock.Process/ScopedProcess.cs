using System.Diagnostics;
using System.Runtime.InteropServices;

namespace AlarmClock.Process;

public enum KillSignal
{
    SIGINT = 2,
    SIGKILL = 9,
    SIGTERM = 15
}

public sealed partial class ScopedProcess : IAsyncDisposable
{
    [LibraryImport("libc", EntryPoint = "kill", SetLastError =true)]
    private static partial int Kill(int pid, int sig);
    
    private readonly int _disposeWaitTimeout;
    private readonly CancellationToken _cancellationToken;

    public System.Diagnostics.Process Process { get; }
    public Stream StandardInput => Process.StandardInput.BaseStream;
    public Stream StandardOutput => Process.StandardOutput.BaseStream;

    public ScopedProcess(ProcessStartInfo startInfo, int disposeWaitTimeout = 30000, CancellationToken cancellationToken = default)
    {
        _disposeWaitTimeout = disposeWaitTimeout;
        _cancellationToken = cancellationToken;
        Process = System.Diagnostics.Process.Start(startInfo) ?? throw new Exception($"Unable to start process {startInfo.FileName}");
    }

    public async Task TerminateAsync(int killMilliseconds, KillSignal signal, CancellationToken cancellationToken)
    {
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(killMilliseconds);
        
        if (Process.StartInfo.RedirectStandardInput)
            Process.StandardInput.Close();
        
        var rc = Kill(Process.Id, (int)signal);
        if (rc != 0)
            throw new Exception($"Unable to kill process {Process.Id} with signal {signal}: {rc}");

        var drainTasks = new List<Task>();
            
        if (Process.StartInfo.RedirectStandardOutput)
            drainTasks.Add(Process.StandardOutput.BaseStream.DrainAsync(8096, cts.Token));
        
        if (Process.StartInfo.RedirectStandardError)
            drainTasks.Add(Process.StandardError.BaseStream.DrainAsync(8096, cts.Token));
        
        try
        {
            await Task.WhenAll(drainTasks);
            await Process.WaitForExitAsync(cts.Token);
        }
        finally
        {
            if (!Process.HasExited)
                Process.Kill(true);
            
            if (Process.StartInfo.RedirectStandardOutput)
                Process.StandardOutput.Close();
                
            if (Process.StartInfo.RedirectStandardError)
                Process.StandardError.Close();
            
            await Process.WaitForExitAsync(CancellationToken.None);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (!_cancellationToken.IsCancellationRequested && Process.WaitForExit(_disposeWaitTimeout))
        {
            Process.Dispose();
            return;
        }
        
        await TerminateAsync(5000, KillSignal.SIGTERM, CancellationToken.None);
        Process.Dispose();
    }
}