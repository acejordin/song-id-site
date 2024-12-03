namespace song_id
{
    public class NetRadio : IAudioSource, IDisposable
    {
        public NetRadio(string url)
        {
            Url = url;

            //Bass.Init(0); //init to 0/"no sound" device, since we're decoding, not playing the music
            //Bass.Configure(Configuration.NetBufferLength, 2000); //set stream buffer size to 2 seconds
            //Bass.Configure(Configuration.NetPreBuffer, 0); //PreBuffer percent to zero so we can start accessing the buffer sooner

            ////Open the stream, set Decode and Float flags, decode so we can read the data without playing it, and Float to get the data as float values instead of bytes, which is needed
            ////for song identification
            //Stream = Bass.CreateStream(Url, 0, BassFlags.Decode | BassFlags.Float | BassFlags.Mono, null, IntPtr.Zero);

            //Bass.ChannelSetSync(Stream, SyncFlags.End, 0, EndSync);

            //ChannelInfo info = Bass.ChannelGetInfo(Stream);
            //Channels = info.Channels;
            //SampleRate = info.Frequency;
        }

        public string Url { get; } = string.Empty;

        public int Stream { get; } = 0;

        public event DataAvailableHandler? DataAvailable;

        public int Channels { get; }
        public int SampleRate { get; }

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            Task.Run(() => 
            {
                int length;
                float[] buffer;

                //var filePath = Path.Combine(".", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");
                //WaveFileWriter waveFileWriter = new WaveFileWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), WaveFormat.FromChannel(stream));

                //int iterations = 0;

                while (true)
                {
                    //long bufferPos = Bass.StreamGetFilePosition(Stream, FileStreamPosition.Buffer);
                    //long endPos = Bass.StreamGetFilePosition(Stream, FileStreamPosition.End);
                    //var progress = Bass.StreamGetFilePosition(Stream, FileStreamPosition.Buffer) * 100 / Bass.StreamGetFilePosition(Stream, FileStreamPosition.End);

                    //double bufferLengthSecs = Bass.ChannelBytes2Seconds(Stream, Bass.StreamGetFilePosition(Stream, FileStreamPosition.Buffer));
                    ////Console.WriteLine($"buffer:{bufferPos},end:{endPos},bufferSecs:{bufferLengthSecs},%:{progress}");

                    //if (Bass.StreamGetFilePosition(Stream, FileStreamPosition.Connected) == 1) //check we're still connected to the station
                    //{
                    //    //++iterations;
                    //    //Console.WriteLine($"buffer:{bufferPos},end:{endPos},bufferSecs:{bufferLengthSecs},%:{progress}");

                    //    length = (int)Bass.StreamGetFilePosition(Stream, FileStreamPosition.Buffer); //get how much buffered data is available
                    //                                                                                 //length = 10000;
                    //    buffer = new float[length];
                    //    length = Bass.ChannelGetData(Stream, buffer, length); //get available data from buffer

                    //    //waveFileWriter.Write(buffer, length); //write data to wav file
                    //    DataAvailable?.Invoke(buffer, length);

                    //    //Console.WriteLine($"Read {length} bytes, iteration:{iterations}");

                    //    if (length < endPos) //if buffer wasn't full, slow down a beat
                    //        Thread.Sleep(500);
                    //}
                }
            });
        }

        public void Stop()
        {
            //Bass.StreamFree(Stream);
        }

        void EndSync(int Handle, int Channel, int Data, IntPtr User)
        {
            Stop();
        }
    }
}
