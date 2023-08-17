using System.Diagnostics;
using Microsoft.VisualBasic;
using NAudio.CoreAudioApi;
using NAudio.Wave;

class Program
{
    static IEnumerable<MMDevice> CaptureDevices { get; set; }
    static void Main(string[] args)
    {
        Console.WriteLine("SPACE - tag, Q - quit");

        while(true) {
            var key = Console.ReadKey(true);

            var enumerator = new MMDeviceEnumerator();
            CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToArray();

            if(Char.ToLower(key.KeyChar).ToString().ToLower() == "q")
                break;

            if(key.Key == ConsoleKey.Spacebar) {
                Console.Write("Listening... ");

                try {
                    var result = CaptureAndTag();

                    if(result.Success) {
                        Console.CursorLeft = 0;
                        Console.WriteLine($"{result.Title} - {result.Artist} : {result.Url}");
                        //Process.Start("explorer", result.Url);
                    } else {
                        Console.WriteLine(":(");
                    }
                } catch(Exception x) {
                    Console.WriteLine("error: " + x.Message);
                }
            }
        }
    }

    static ShazamResult CaptureAndTag() {
        Trace.WriteLine("Starting Capture...");
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");

        using (var capture = new WasapiLoopbackCapture()) {
            var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };
            //captureBuf.BufferLength = capture.WaveFormat.AverageBytesPerSecond * 30; //30 sec buffer

            capture.DataAvailable += (s, e) => {
                 captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            capture.StartRecording();

            using(var resampler = new MediaFoundationResampler(captureBuf, new WaveFormat(Analysis.SAMPLE_RATE, 16, 1))) {
                var sampleProvider = resampler.ToSampleProvider();
                var retryMs = 2000;
                var tagId = Guid.NewGuid().ToString();
                //Int64 loop = 0;
                while(true) {
                    //Trace.WriteLine($"Loop: {++loop}");
                    //Trace.WriteLine(captureBuf.BufferedDuration.TotalSeconds);
                    while(captureBuf.BufferedDuration.TotalSeconds < 1)
                        Thread.Sleep(100);

                    analysis.ReadChunk(sampleProvider);

                    if(analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                        finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);
                    if(analysis.ProcessedMs >= retryMs) {
                        new Painter(analysis, finder).Paint(Path.Combine(outFolder, $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}-spectro-naudio.png"));
                        //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");
                        //throw new Exception();

                        var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                        Trace.WriteLine("Sending to Shazam...");
                        var result = ShazamApi.SendRequest(tagId, analysis.ProcessedMs, sigBytes).GetAwaiter().GetResult();
                        //Trace.WriteLine("Got result!");
                        if (result.Success)
                            return result;

                        retryMs = result.RetryMs;
                        if (retryMs == 0)
                            return result;
                    }
                }
            }
        }
    }
}