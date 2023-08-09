using System.Diagnostics;
using NAudio.CoreAudioApi;
using NAudio.Wave;

class Program
{
    static IEnumerable<MMDevice> CaptureDevices { get; set; }
    static void Main(string[] args)
    {
        Console.WriteLine("SPACE - tag, E - Enumerate Audio Devices, Q - quit");

        while(true) {
            var key = Console.ReadKey(true);

            var enumerator = new MMDeviceEnumerator();
            CaptureDevices = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToArray();

            if(Char.ToLower(key.KeyChar) == 'q')
                break;
            if(Char.ToLower(key.KeyChar) == 'e')
            {
                //var enumerator = new MMDeviceEnumerator();
                //var captureEndpoints = enumerator.EnumerateAudioEndPoints(DataFlow.Capture, DeviceState.Active).ToArray();
                var allEndpoints = enumerator.EnumerateAudioEndPoints(DataFlow.All, DeviceState.Active).ToArray();
            }

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
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        using(var capture = new WasapiLoopbackCapture()) {
            var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };

            capture.DataAvailable += (s, e) => {
                 captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
            };

            capture.StartRecording();

            using(var resampler = new MediaFoundationResampler(captureBuf, new WaveFormat(Analysis.SAMPLE_RATE, 16, 1))) {
                var sampleProvider = resampler.ToSampleProvider();
                var retryMs = 3000;
                var tagId = Guid.NewGuid().ToString();

                while(true) {
                    while(captureBuf.BufferedDuration.TotalSeconds < 1)
                        Thread.Sleep(100);

                    analysis.ReadChunk(sampleProvider);

                    if(analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                        finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

                    if(analysis.ProcessedMs >= retryMs) {
                        //new Painter(analysis, finder).Paint("c:/temp/spectro.png");
                        //new Synthback(analysis, finder).Synth("c:/temp/synthback.raw");

                        var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                        var result = ShazamApi.SendRequest(tagId, analysis.ProcessedMs, sigBytes).GetAwaiter().GetResult();
                        if(result.Success)
                            return result;

                        retryMs = result.RetryMs;
                        if(retryMs == 0)
                            return result;
                    }
                }
            }
        }
    }
}