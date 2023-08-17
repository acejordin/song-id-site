using ManagedBass;
using System.Diagnostics;

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
    private byte[] _buffer { get; set; }
    private int _writePostionIdx = 0;
    private int _readPositionIdx = 0;

    private int _averageBytesPerSecond = 16000 * 2; //simplified in this case, 16000hz sample rate, 16bit audio, so 2 bytes per sample

    public SampleProvider()
    {
        _buffer = new byte[_averageBytesPerSecond * 30]; //30 second buffer
    }

    public TimeSpan BufferedDuration 
    { 
        get
        {
            //This is hard coded to assume the audio stream is 16000hz 16 bit mono.
            int channels = 1;
            int bitsPerSample = 16;
            int sampleRate = 16000;
            short blockAlign = (short)(channels * (bitsPerSample / 8));
            //short blockAlign = (short)channels;
            int averageBytesPerSecond = sampleRate * blockAlign;

            TimeSpan bufferedDuration = TimeSpan.FromSeconds((_writePostionIdx - _readPositionIdx) / (double)averageBytesPerSecond);
            //Trace.WriteLine($"BufferedDuration: {bufferedDuration.TotalSeconds} secs");
            return bufferedDuration;
        }
    }

    public void Write(byte[] buffer, int length)
    {
        Array.Copy(buffer, 0, _buffer, _writePostionIdx, buffer.Length);
        _writePostionIdx += buffer.Length;
    }

    /// <summary>
    /// Not implemented because it isn't needed by Analysis.ReadChunk()
    /// </summary>
    public WaveFormat WaveFormat => throw new NotImplementedException();
    //public WaveFormat WaveFormat => WaveFormat.CreateIeeeFloat(16000, 1);

    public int Read(float[] buffer, int offset, int count)
    {
        int sourceBytesRequired = count * 2;
        //EnsureSourceBuffer(sourceBytesRequired);
        int bytesRead = Math.Min(sourceBytesRequired, _writePostionIdx - _readPositionIdx);
        int outIndex = offset;
        for (int n = 0; n < bytesRead; n += 2)
        {
            buffer[outIndex++] = BitConverter.ToInt16(_buffer, n + _readPositionIdx) / 32768f;
        }
        _readPositionIdx += sourceBytesRequired;
        return bytesRead / 2;
    }
}