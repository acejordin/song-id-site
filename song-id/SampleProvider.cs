using ManagedBass;

/// <summary>
/// Like IWaveProvider, but makes it much simpler to put together a 32 bit floating
/// point mixing engine
/// </summary>
public interface ISampleProvider
{
    /// <summary>
    /// Gets the WaveFormat of this Sample Provider.
    /// </summary>
    /// <value>The wave format.</value>
    WaveFormat WaveFormat { get; }

    /// <summary>
    /// Fill the specified buffer with 32 bit floating point samples
    /// </summary>
    /// <param name="buffer">The buffer to fill with samples.</param>
    /// <param name="offset">Offset into buffer</param>
    /// <param name="count">The number of samples to read</param>
    /// <returns>the number of samples written to the buffer.</returns>
    int Read(float[] buffer, int offset, int count);
}

public class SampleProvider : ISampleProvider
{
    private float[] _bufferFloat { get; set; }
    private int _writePostionIdx = 0;
    private int _readPositionIdx = 0;

    private int _averageBytesPerSecond = 44100; //simplified in this case

    public SampleProvider(int channels, int sampleRate)
    {
        _averageBytesPerSecond = channels * sampleRate;
        _bufferFloat = new float[_averageBytesPerSecond * 100000]; //20 second buffer
    }

    public TimeSpan BufferedDuration
    {
        get
        {
            //This is hard coded to assume the audio stream is 16000hz 32 bit mono.
            //int channels = 1;
            //int bitsPerSample = 32;
            //int sampleRate = 44100;
            //short blockAlign = (short)(channels * (bitsPerSample / 8));
            //short blockAlign = (short)channels;
            //int averageBytesPerSecond = sampleRate * blockAlign;

            TimeSpan bufferedDuration = TimeSpan.FromSeconds((_writePostionIdx - _readPositionIdx) / (double)_averageBytesPerSecond);
            //Trace.WriteLine($"BufferedDuration: {bufferedDuration.TotalSeconds} secs");
            return bufferedDuration;
        }
    }

    public void Write(float[] buffer, int length)
    {
        Array.Copy(buffer, 0, _bufferFloat, _writePostionIdx, buffer.Length);
        _writePostionIdx += buffer.Length;
    }

    /// <summary>
    /// Not implemented because it isn't needed by Analysis.ReadChunk()
    /// </summary>
    public WaveFormat WaveFormat => throw new NotImplementedException();

    public int Read(float[] buffer, int offset, int count)
    {
        int toRead = Math.Min(count, _writePostionIdx - _readPositionIdx);
        Array.Copy(_bufferFloat, _readPositionIdx, buffer, offset, toRead);
        _readPositionIdx += toRead;
        return toRead;
    }
}