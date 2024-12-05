using NAudio.Dsp;
using NAudio.Wave;
using NAudio.Wave.SampleProviders;
using NLayer.NAudioSupport;
using System.Diagnostics;
using System.Net;
using System.Reflection.PortableExecutable;

namespace song_id
{
    public class NetRadio : IAudioSource, IDisposable
    {
        enum StreamingPlaybackState
        {
            Stopped,
            Playing,
            Buffering,
            Paused
        }

        private BufferedWaveProvider bufferedWaveProvider;
        private IWavePlayer waveOut;
        private volatile StreamingPlaybackState playbackState;
        private volatile bool fullyDownloaded;
        private static HttpClient httpClient;

        public NetRadio(string url)
        {
            Url = url;

            //load the Url and get the Channels and SampleRate
            if (httpClient == null) httpClient = new HttpClient();

            using (Stream stream = httpClient.GetStreamAsync(Url).Result)
            using (var readFullyStream = new ReadFullyStream(stream))
            {
                Mp3Frame frame;
                try
                {
                    frame = Mp3Frame.LoadFromStream(readFullyStream);
                    Channels = frame.ChannelMode == ChannelMode.Mono ? 1 : 2;
                    SampleRate = frame.SampleRate;
                }
                catch (Exception)
                {
                    throw;
                }
            }
        }

        private static IMp3FrameDecompressor CreateFrameDecompressor(Mp3Frame frame)
        {
            WaveFormat waveFormat = new Mp3WaveFormat(frame.SampleRate, frame.ChannelMode == ChannelMode.Mono ? 1 : 2,
                frame.FrameLength, frame.BitRate);

            return new Mp3FrameDecompressor(waveFormat);
        }

        public string Url { get; } = string.Empty;

        public int Stream { get; } = 0;

        public event DataAvailableHandler? DataAvailable;

        public int Channels { get; private set; }
        public int SampleRate { get; private set; }

        //private bool IsBufferNearlyFull
        //{
        //    get
        //    {
        //        return bufferedWaveProvider != null &&
        //               bufferedWaveProvider.BufferLength - bufferedWaveProvider.BufferedBytes
        //               < bufferedWaveProvider.WaveFormat.AverageBytesPerSecond / 4;
        //    }
        //}

        public void Dispose()
        {
            Stop();
        }

        public void Start()
        {
            Task.Run(() =>
            {
                fullyDownloaded = false;
                if (httpClient == null) httpClient = new HttpClient();
                Stream stream;
                try
                {
                    stream = httpClient.GetStreamAsync(Url).Result;
                }
                catch (Exception e)
                {
                    throw;
                }
                var buffer = new byte[16384 * 4]; // needs to be big enough to hold a decompressed frame
                var floatBuffer = new float[16384 * 4];

                IMp3FrameDecompressor? decompressor = null;
                var resampler = new WdlResampler();
                resampler.SetMode(true, 2, false);
                resampler.SetFilterParms();
                resampler.SetFeedMode(true); // input driven
                try
                {
                    using (stream)
                    {
                        var readFullyStream = new ReadFullyStream(stream);
                        do
                        {
                            //if (IsBufferNearlyFull)
                            //{
                            //    Debug.WriteLine("Buffer getting full, taking a break");
                            //    Thread.Sleep(500);
                            //}
                            //else
                            //{
                            //Debug.WriteLine($"BufferedSecs:{bufferedWaveProvider?.BufferedDuration.TotalSeconds}");
                            Mp3Frame frame;
                            try
                            {
                                frame = Mp3Frame.LoadFromStream(readFullyStream);
                                Channels = frame.ChannelMode == ChannelMode.Mono ? 1 : 2;
                                SampleRate = frame.SampleRate;
                            }
                            catch (EndOfStreamException)
                            {
                                fullyDownloaded = true;
                                // reached the end of the MP3 file / stream
                                break;
                            }
                            catch (WebException)
                            {
                                // probably we have aborted download from the GUI thread
                                break;
                            }
                            if (frame == null) break;
                            if (decompressor == null)
                            {
                                // don't think these details matter too much - just help ACM select the right codec
                                // however, the buffered provider doesn't know what sample rate it is working at
                                // until we have a frame
                                decompressor = CreateFrameDecompressor(frame);
                                bufferedWaveProvider = new BufferedWaveProvider(decompressor.OutputFormat);
                                //bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

                                //bufferedWaveProvider.BufferDuration =
                                //    TimeSpan.FromSeconds(20); // allow us to get well ahead of ourselves
                                //                              //this.bufferedWaveProvider.BufferedDuration = 250;

                                resampler.SetRates(frame.SampleRate, 16000);

                            }
                            int decompressed = decompressor.DecompressFrame(frame, buffer, 0);
                            //Debug.WriteLine(String.Format("Decompressed a frame {0}", decompressed));
                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);
                            //int length = resampler.Read(floatBuffer, 0, decompressed);
                            //DataAvailable?.Invoke(floatBuffer, length);

                            int channels = frame.ChannelMode == ChannelMode.Mono ? 1 : 2;
                            int framesAvailable = decompressed / channels;
                            float[] inBuffer;
                            int inBufferOffset;
                            int inNeeded = resampler.ResamplePrepare(framesAvailable, channels, out inBuffer, out inBufferOffset);

                            bufferedWaveProvider.AddSamples(buffer, 0, decompressed);


                            Array.Copy(buffer, 0, inBuffer, inBufferOffset, inNeeded * channels);

                            //var bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat());
                            //bufferedWaveProvider.ToSampleProvider().ToMono().ToWaveProvider();

                            int inAvailable = inNeeded;
                            float[] outBuffer = new float[2000]; // plenty big enough
                            int framesRequested = outBuffer.Length / channels;
                            int outAvailable = resampler.ResampleOut(outBuffer, 0, inAvailable, framesRequested, channels);

                            if (outAvailable != 0)
                                DataAvailable?.Invoke(outBuffer, outAvailable * channels);

                            playbackState = StreamingPlaybackState.Playing;
                            //}

                        } while (playbackState != StreamingPlaybackState.Stopped);
                        Debug.WriteLine("Exiting");
                        // was doing this in a finally block, but for some reason
                        // we are hanging on response stream .Dispose so never get there
                        decompressor.Dispose();
                    }
                }
                finally
                {
                    if (decompressor != null)
                    {
                        decompressor.Dispose();
                    }
                }
            });
        }

        public void Stop()
        {
            //Bass.StreamFree(Stream);
        }
    }
}
