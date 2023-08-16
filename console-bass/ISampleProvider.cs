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
    private List<float> _bufferFloat { get; set; } = new List<float>();
    private int _readPositionIdx = 0;

    public void Write(List<float> buffer, int length)
    {
        throw new NotImplementedException();
    }

    public void Write(float[] buffer, int length)
    {
        for(int i = 0; i < buffer.Length; i++)
        {
            _bufferFloat.Add(buffer[i]);
        }
    }

    public WaveFormat WaveFormat => throw new NotImplementedException();

    public int Read(float[] buffer, int offset, int count)
    {
        int outIdx = offset;
        for (int i = 0; i < count; i++)
        {
            if (i + offset > _bufferFloat.Count - 1)
                break;

            buffer[outIdx++] = _bufferFloat[_readPositionIdx++];
        }
        return count;
    }
}