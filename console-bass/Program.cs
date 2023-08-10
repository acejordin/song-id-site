using ManagedBass;

class Program
{
    static void Main(string[] args)
    {
        Console.WriteLine("Select Audio Device");

        List<RecordingDevice> availableAudioSources = RecordingDevice.Enumerate().ToList();
        availableAudioSources.ForEach(src => Console.WriteLine($"[{src.Index}] {src.ToString()}"));

        Console.Write("Enter num [0]: ");
        int audIdx = Int32.Parse(Console.ReadLine() ?? "0");
        RecordingDevice recordingDevice = availableAudioSources[audIdx];

        Console.WriteLine();
        Console.WriteLine("SPACE - tag, Q - quit");

        while (true)
        {
            var key = Console.ReadKey(true);

            if (Char.ToLower(key.KeyChar).ToString().ToLower() == "q")
                break;

            if (key.Key == ConsoleKey.Spacebar)
            {
                Console.Write("Listening... ");

                try
                {
                    var result = CaptureAndTag(recordingDevice, sampleRate:16000, channels:1);

                    if (result.Success)
                    {
                        Console.CursorLeft = 0;
                        Console.WriteLine($"{result.Title} - {result.Artist} : {result.Url}");
                        //Process.Start("explorer", result.Url);
                    }
                    else
                    {
                        Console.WriteLine(":(");
                    }
                }
                catch (Exception x)
                {
                    Console.WriteLine("error: " + x.Message);
                }
            }
        }
    }

    static ShazamResult CaptureAndTag(RecordingDevice recordingDevice, int sampleRate = 44100, int channels = 2, int bitsPerSample = 16)
    {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        AudioRecorder audioRecorder = new AudioRecorder(recordingDevice, sampleRate, channels, bitsPerSample);

        string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");
        var filePath = Path.Combine(outFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");
        WaveFileWriter waveFileWriter = new WaveFileWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), new WaveFormat(sampleRate, bitsPerSample, channels));

        audioRecorder.DataAvailable += (Buffer, Length) => waveFileWriter?.Write(Buffer, Length);
        audioRecorder.Start();

        var retryMs = 3000;
        var tagId = Guid.NewGuid().ToString();

        Thread.Sleep(5000);

        audioRecorder.Stop();
        audioRecorder?.Dispose();

        waveFileWriter?.Dispose();

        //while (true)
        //{
        //    while (captureBuf.BufferedDuration.TotalSeconds < 1)
        //        Thread.Sleep(100);

        //    analysis.ReadChunk(sampleProvider);

        //    if (analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
        //        finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

        //    if (analysis.ProcessedMs >= retryMs)
        //    {
        //        //new Painter(analysis, finder).Paint("c:/temp/spectro.png");
        //        //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");

        //        var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
        //        var result = ShazamApi.SendRequest(tagId, analysis.ProcessedMs, sigBytes).GetAwaiter().GetResult();
        //        if (result.Success)
        //            return result;

        //        retryMs = result.RetryMs;
        //        if (retryMs == 0)
        //            return result;
        //    }
        //}
        return new ShazamResult { Artist = "in the ambulance" };
    }
}