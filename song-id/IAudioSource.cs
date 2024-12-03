namespace song_id
{
    public interface IAudioSource : IDisposable
    {
        public event DataAvailableHandler? DataAvailable;
        public void Start();
        public void Stop();
        public int SampleRate { get; }
        public int Channels { get; }
    }
}
