namespace AlarmClock.Process;

internal static class StreamExtension
{
    public static async Task DrainAsync(this Stream source, int bufferSize, CancellationToken cancellationToken)
    {
        var buffer = new Memory<byte>(new byte[bufferSize]);
        while (await source.ReadAsync(buffer, cancellationToken) > 0){}
    }
}