﻿using Microsoft.Extensions.Logging;
using NAudio.Wave;
using System.Diagnostics;

namespace song_id
{
    public class SongId
    {
        private IAudioSource _audioSource;
        private readonly ILogger _logger;
        private int _deadAirLengthSecs;

        public SongId(IAudioSource audioSource, ILogger logger, int deadAirLengthSecs = 10)
        {
            _audioSource = audioSource;
            _logger = logger;
            _deadAirLengthSecs = deadAirLengthSecs;
        }

        public async Task<ShazamResult> CaptureAndTagAsync(CancellationToken cancellationToken = default)
        {
            var analysis = new Analysis();
            var finder = new LandmarkFinder(analysis);

            string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");
            //string outFolder = "/app";
            var filePath = Path.Combine(outFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");

            DateTime lastNoiseDetected = DateTime.Now;
            //SampleProvider sampleProvider = new SampleProvider();
            BufferedWaveProvider bufferedWaveProvider = new BufferedWaveProvider(new WaveFormat(16000, 16, 1));

            int iterations = 0;
            _audioSource.DataAvailable += (Buffer, Length) =>
            {
                ++iterations;
                //Debug.WriteLine($"DataAvailable() called");
                float agg = Buffer.Aggregate((acc, x) => acc + x);
                float avg = agg / Buffer.Length;
                //Debug.WriteLine($"Buffer average noise: {avg:0.###############}");

                if (avg >= 0.000001 || avg <= -0.000001)
                {
                    lastNoiseDetected = DateTime.Now;
                }
                else
                {
                    Debug.WriteLine($"Think it's dead air");
                }

                //Debug.WriteLine("Calling sampleProvider.Write()");
                //for (int i = 0; i < Buffer.Length; i++) { Buffer[i] = Buffer[i]; }
                //waveFileWriter?.Write(Buffer, Length);
                bufferedWaveProvider.AddSamples(Buffer, 0, Length);
            };

            _audioSource.Start();

            var retryMs = 3000;
            var tagId = Guid.NewGuid().ToString();

            try
            {
                while (true)
                {
                    Debug.WriteLine($"BufferedDuration.TotalSeconds: {sampleProvider.BufferedDuration.TotalSeconds}");
                    //don't start analyzing until there is at least a second of audio recorded
                    while (sampleProvider.BufferedDuration.TotalSeconds < 1)
                    {
                        //Debug.WriteLine($"BufferedDuration.TotalSeconds: {sampleProvider.BufferedDuration.TotalSeconds}");
                        //if (cancellationToken.IsCancellationRequested || DateTime.Now - lastNoiseDetected > new TimeSpan(0, 0, _deadAirLengthSecs))
                        //{
                        //    Debug.WriteLine($"Cancel Request: {cancellationToken.IsCancellationRequested}, Dead air length: {DateTime.Now - lastNoiseDetected}");
                        //    return new ShazamResult { Success = false, Title = "Dead Air" };
                        //}

                        await Task.Delay(100, cancellationToken);
                    }

                    //start reading in the audio to analyze
                    analysis.ReadChunk(sampleProvider);

                    //start looking for landmarks in the audio once there are enough stripes
                    if (analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                        finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

                    //once there is enough audio processed send to Shazam
                    if (analysis.ProcessedMs >= retryMs)
                    {
                        _logger.LogInformation($"analysis.ProcessedMs: {analysis.ProcessedMs}");

                        new Painter(analysis, finder).Paint(Path.Combine(outFolder, $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}-spectro-bass.png"));

                        var sigBytes = Sig.Write(Analysis.SAMPLE_RATE, analysis.ProcessedSamples, finder);
                        //throw new Exception();

                        _logger.LogInformation("Sending to Shazam...");
                        var result = await new ShazamApi(_logger).SendRequestAsync(tagId, analysis.ProcessedMs, sigBytes, cancellationToken);
                        if (result.Success)
                            return result;

                        //RetryMs from Shazam means that is how much processed audio to send in next request, not how long to wait before retrying
                        //Sending more than around 12 secs of processed audio to Spotify seems to cause Shazam to not identify the song
                        retryMs = result.RetryMs;
                        _logger.LogInformation($"ShazamResult.RetryMs: {result.RetryMs}");

                        if (result.RetryMs == 0)
                            return result;
                    }
                }
            }
            finally
            {
                _audioSource.Stop();
            }
        }
    }
}