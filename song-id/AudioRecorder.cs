﻿using ManagedBass;
using song_id;
using System.Runtime.InteropServices;

public class AudioRecorder : IAudioSource, IDisposable
{
    int _device, _handle;

    public AudioRecorder(RecordingDevice device, int frequency, int channels)
    {
        _device = device.Index;

        Bass.RecordInit(_device);

        //_handle = Bass.RecordStart(44100, 2, BassFlags.RecordPause, Procedure);
        _handle = Bass.RecordStart(frequency, channels, BassFlags.RecordPause | BassFlags.Float, Procedure);
        Channels = channels;
        SampleRate = frequency;
    }

    float[]? _buffer;

    public int Channels { get; }
    public int SampleRate { get; }

    bool Procedure(int Handle, IntPtr Buffer, int Length, IntPtr User)
    {
        if (_buffer == null || _buffer.Length != Length / 4)
            _buffer = new float[Length / 4];

        Marshal.Copy(Buffer, _buffer, 0, _buffer.Length);

        DataAvailable?.Invoke(_buffer, Length);

        return true;
    }

    public event DataAvailableHandler? DataAvailable;

    public void Start()
    {
        Bass.ChannelPlay(_handle);
    }

    public void Stop()
    {
        Bass.ChannelStop(_handle);
    }

    public void Dispose()
    {
        Bass.CurrentRecordingDevice = _device;

        Bass.RecordFree();
    }
}

