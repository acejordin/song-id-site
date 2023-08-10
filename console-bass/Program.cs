﻿using ManagedBass;

class Program
{
    static List<RecordingDevice> AvailableAudioSources { get; set; } = new List<RecordingDevice>();
    static void Main(string[] args)
    {
        Console.WriteLine("Select Audio Device");

        AvailableAudioSources = RecordingDevice.Enumerate().ToList();
        AvailableAudioSources.ForEach(src => Console.WriteLine($"[{src.Index}] {src.ToString()}"));

        Console.Write("Enter num: ");
        var key = Console.ReadKey();
        Console.WriteLine();
        Console.WriteLine("SPACE - tag, Q - quit");

        while (true)
        {
            key = Console.ReadKey(true);

            if (Char.ToLower(key.KeyChar).ToString().ToLower() == "q")
                break;

            if (key.Key == ConsoleKey.Spacebar)
            {
                Console.Write("Listening... ");

                try
                {
                    var result = CaptureAndTag();

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

    static ShazamResult CaptureAndTag()
    {
        var analysis = new Analysis();
        var finder = new LandmarkFinder(analysis);

        //using (var capture = new WasapiLoopbackCapture())
        //{
        //var captureBuf = new BufferedWaveProvider(capture.WaveFormat) { ReadFully = false };

        //capture.DataAvailable += (s, e) => {
        //    captureBuf.AddSamples(e.Buffer, 0, e.BytesRecorded);
        //};
        //capture.StartRecording();

        //using (var resampler = new MediaFoundationResampler(captureBuf, new WaveFormat(Analysis.SAMPLE_RATE, 16, 1)))
        //{
        //var sampleProvider = resampler.ToSampleProvider();




        //var retryMs = 3000;
        //var tagId = Guid.NewGuid().ToString();

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



        //}
        //}
    }
}