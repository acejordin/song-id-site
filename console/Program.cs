using ManagedBass;
using NAudio.CoreAudioApi;
//using NAudio.Wave;
using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading.Channels;

class Program
{
    static IEnumerable<MMDevice> CaptureDevices { get; set; }

    static void Main(string[] args)
    {
        Bass.Init(0); //init to 0/"no sound" device, since we're decoding, not playing the music
        Bass.Configure(Configuration.NetBufferLength, 2000); //set stream buffer size to 2 seconds
        Bass.Configure(Configuration.NetPreBuffer, 0); //PreBuffer percent to zero so we can start accessing the buffer sooner

        //var file = new FileStream("afile.mp3", FileMode.OpenOrCreate, FileAccess.Write, FileShare.Read);
        //byte[] _buffer = new byte[0];

        //void MyDownloadProc(IntPtr buffer, int length, IntPtr user)
        //{
        //    if (buffer == IntPtr.Zero)
        //    {
        //        // finished downloading
        //        file.Close();
        //        return;
        //    }

        //    if (_buffer == null || _buffer.Length < length)
        //        _buffer = new byte[length];

        //    Marshal.Copy(buffer, _buffer, 0, length);

        //    file.Write(_buffer, 0, length);
        //}

        //void Downloaded(int Handle, int Channel, int Data, IntPtr User)
        //{
        //    Console.WriteLine("Downloaded() called");
        //}

        Task.Factory.StartNew(() =>
        {
            Console.WriteLine("Opening stream...");
            //Open the stream, set Decode and Float flags, decode so we can read the data without playing it, and Float to get the data as float values instead of bytes, which is needed
            //for song identification
            var stream = Bass.CreateStream("https://live.ukrp.tv/outreachradio.mp3", 0, BassFlags.Decode | BassFlags.Float | BassFlags.Mono, null, IntPtr.Zero);

            Bass.ChannelSetSync(stream, SyncFlags.End, 0, EndSync);

            //Bass.ChannelSetSync(stream, SyncFlags.Downloaded, 0, Downloaded);

            //Bass.ChannelPlay(stream);

            int length;
            float[] buffer;

            var filePath = Path.Combine(".", DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");
            WaveFileWriter waveFileWriter = new WaveFileWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), WaveFormat.FromChannel(stream));

            int iterations = 0;

            while (true)
            {
                long bufferPos = Bass.StreamGetFilePosition(stream, FileStreamPosition.Buffer);
                long endPos = Bass.StreamGetFilePosition(stream, FileStreamPosition.End);
                var progress = Bass.StreamGetFilePosition(stream, FileStreamPosition.Buffer) * 100 / Bass.StreamGetFilePosition(stream, FileStreamPosition.End);

                ChannelInfo info = Bass.ChannelGetInfo(stream);
                double bufferLengthSecs = Bass.ChannelBytes2Seconds(stream, Bass.StreamGetFilePosition(stream, FileStreamPosition.Buffer));
                Console.WriteLine($"buffer:{bufferPos},end:{endPos},bufferSecs:{bufferLengthSecs},%:{progress}");

                // (progress >= 100 || Bass.StreamGetFilePosition(stream, FileStreamPosition.Connected) == 0)
                if (Bass.StreamGetFilePosition(stream, FileStreamPosition.Connected) == 1) //check we're still connected to the station
                {
                    ++iterations;
                    //Console.WriteLine($"buffer:{bufferPos},end:{endPos},bufferSecs:{bufferLengthSecs},%:{progress}");

                    //length = (int)Bass.ChannelGetLength(stream, PositionFlags.Bytes);
                    //length = Bass.ChannelGetData(stream, IntPtr.Zero, (int)DataFlags.Available);
                    length = (int)Bass.StreamGetFilePosition(stream, FileStreamPosition.Buffer); //get how much buffered data is available
                    //length = 10000;
                    buffer = new float[length];
                    length = Bass.ChannelGetData(stream, buffer, length); //get available data from buffer

                    waveFileWriter.Write(buffer, length); //write data to wav file

                    Console.WriteLine($"Read {length} bytes, iteration:{iterations}");

                    if(length < endPos) //if buffer wasn't full, slow down a beat
                        Thread.Sleep(500);
                }
            }
        });

        void EndSync(int Handle, int Channel, int Data, IntPtr User)
        {

        }

        Console.ReadLine();
    }

    

    //static void Main(string[] args)
    //{
    //    Console.WriteLine("SPACE - tag, Q - quit");

    //    while(true) {
    //        var key = Console.ReadKey(true);

    //        var enumerator = new MMDeviceEnumerator();
    //        CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToArray();

    //        if(Char.ToLower(key.KeyChar).ToString().ToLower() == "q")
    //            break;

    //        if(key.Key == ConsoleKey.Spacebar) {
    //            Console.Write("Listening... ");

    //            try {
    //                var result = CaptureAndTag();

    //                if(result.Success) {
    //                    Console.CursorLeft = 0;
    //                    Console.WriteLine($"{result.Title} - {result.Artist} : {result.Url}");
    //                    //Process.Start("explorer", result.Url);
    //                } else {
    //                    Console.WriteLine(":(");
    //                }
    //            } catch(Exception x) {
    //                Console.WriteLine("error: " + x.Message);
    //            }
    //        }
    //    }
    //}

    //static ShazamResult CaptureAndTag() {
    //    Trace.WriteLine("Starting Capture...");
    //    var analysis = new Analysis();
    //    var finder = new LandmarkFinder(analysis);

    //    string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");

    //    using (var capture = new WasapiLoopbackCapture()) {
    //        var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };
    //        //captureBuf.BufferLength = capture.WaveFormat.AverageBytesPerSecond * 30; //30 sec buffer

    //        capture.DataAvailable += (s, e) => {
    //             captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
    //        };

    //        capture.StartRecording();

    //        using(var resampler = new MediaFoundationResampler(captureBuf, new WaveFormat(Analysis.SAMPLE_RATE, 16, 1))) {
    //            var sampleProvider = resampler.ToSampleProvider();
    //            var retryMs = 2000;
    //            var tagId = Guid.NewGuid().ToString();
    //            //Int64 loop = 0;
    //            while(true) {
    //                //Trace.WriteLine($"Loop: {++loop}");
    //                //Trace.WriteLine(captureBuf.BufferedDuration.TotalSeconds);
    //                while(captureBuf.BufferedDuration.TotalSeconds < 1)
    //                    Thread.Sleep(100);

    //                analysis.ReadChunk(sampleProvider);

    //                if(analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
    //                    finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);
    //                if(analysis.ProcessedMs >= retryMs) {
    //                    new Painter(analysis, finder).Paint(Path.Combine(outFolder, $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}-spectro-naudio.png"));
    //                    //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");
    //                    //throw new Exception();

    //                    var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
    //                    Trace.WriteLine("Sending to Shazam...");
    //                    var result = ShazamApi.SendRequest(tagId, analysis.ProcessedMs, sigBytes).GetAwaiter().GetResult();
    //                    //Trace.WriteLine("Got result!");
    //                    if (result.Success)
    //                        return result;

    //                    retryMs = result.RetryMs;
    //                    if (retryMs == 0)
    //                        return result;
    //                }
    //            }
    //        }
    //    }
    //}
}