using ManagedBass;
using System.Diagnostics;

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
                    var result = CaptureAndTag(recordingDevice, sampleRate:16000, channels:1, bitsPerSample:32);

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

        string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");
        var filePath = Path.Combine(outFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");

        using (WaveFileWriter waveFileWriter = new WaveFileWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), WaveFormat.CreateIeeeFloat(sampleRate, channels)))
        using (AudioRecorder audioRecorder = new AudioRecorder(recordingDevice, sampleRate, channels))
        {
            SampleProvider sampleProvider = new SampleProvider();
            audioRecorder.DataAvailable += (Buffer, Length) =>
            {
                for (int i = 0; i < Buffer.Length; i++){ Buffer[i] = Buffer[i]; }
                waveFileWriter?.Write(Buffer, Length);
                sampleProvider.Write(Buffer, Length);
            };

            audioRecorder.Start();

            var retryMs = 3000;
            var tagId = Guid.NewGuid().ToString();

            try
            {
                while (true)
                {
                    //don't start analyzing until there is at least a second of audio recorded
                    while (sampleProvider.BufferedDuration.TotalSeconds < 1)
                        Thread.Sleep(100);

                    //start reading in the audio to analyze
                    analysis.ReadChunk(sampleProvider);

                    //start looking for landmarks in the audio once there are enough stripes
                    if (analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                        finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

                    //once there is enough audio process send to Shazam
                    if (analysis.ProcessedMs >= retryMs)
                    {
                        Trace.WriteLine($"analysis.ProcessedMs: {analysis.ProcessedMs}");

                        new Painter(analysis, finder).Paint(Path.Combine(outFolder, $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}-spectro-bass.png"));

                        var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                        //throw new Exception();

                        Trace.WriteLine("Sending to Shazam...");
                        var result = ShazamApi.SendRequest(tagId, analysis.ProcessedMs, sigBytes).GetAwaiter().GetResult();
                        if (result.Success)
                            return result;

                        retryMs = result.RetryMs;
                        Trace.WriteLine($"ShazamResult.RetryMs: {result.RetryMs}");
                        if (result.RetryMs == 0)
                            return result;
                    }
                }
            }
            finally
            {
                audioRecorder.Stop();
            }
        }
    }
}