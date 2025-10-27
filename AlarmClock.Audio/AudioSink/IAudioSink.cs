namespace AlarmClock.Audio.AudioSink;

public enum AudioEncoding
{
    Signed,
    Unsigned,
    FloatingPoint
}

public record AudioFormat
{
    public AudioFormat(int channels, int sampleRate, int bitsPerSample, AudioEncoding encoding, bool littleEndian, int frameSize = 0)
    {
        Channels = channels;
        SampleRate = sampleRate;
        BitsPerSample = bitsPerSample;
        Encoding = encoding;
        LittleEndian = littleEndian;
        FrameSize = frameSize == 0 ? Channels * BitsPerSample / 8 : frameSize;
    }

    public int FrameSize { get; }
    public int Channels { get; }
    public int SampleRate { get; }
    public int BitsPerSample { get; }
    public AudioEncoding Encoding { get; }
    public bool LittleEndian { get; }
}

// we always expect 2 channels
public interface IAudioSink
{
    AudioFormat Format { get; }
    
    Task WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken cancellationToken);
    
    /// <summary>
    /// Signals end-of-stream for the current playback and waits until all audio has been played.
    /// </summary>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    Task FlushAsync(CancellationToken cancellationToken);
}