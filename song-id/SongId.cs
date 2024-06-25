using ManagedBass;
using Microsoft.Extensions.Logging;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading.Channels;

namespace song_id
{
    public class SongId
    {
        private RecordingDevice _recordingDevice;
        private readonly ILogger _logger;
        private int _sampleRate;
        private int _channels;

        public SongId(RecordingDevice recordingDevice, ILogger logger, int sampleRate = 16000, int channels = 1)
        {
            _recordingDevice = recordingDevice;
            _logger = logger;
            _sampleRate = sampleRate;
            _channels = channels;
        }

        public static List<RecordingDevice> GetAvailableRecordingDevices()
        {
            return RecordingDevice.Enumerate().ToList();
        }

        public async Task<ShazamResult> CaptureAndTagAsync(CancellationToken cancellationToken = default)
        {
            var analysis = new Analysis();
            var finder = new LandmarkFinder(analysis);

            //string outFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MBass\\");
            string outFolder = "/app";
            var filePath = Path.Combine(outFolder, DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss") + ".wav");

            //using (WaveFileWriter waveFileWriter = new WaveFileWriter(new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.Read), WaveFormat.CreateIeeeFloat(_sampleRate, _channels)))
            using (AudioRecorder audioRecorder = new AudioRecorder(_recordingDevice, _sampleRate, _channels))
            {
                SampleProvider sampleProvider = new SampleProvider();
                audioRecorder.DataAvailable += (Buffer, Length) =>
                {
                    //Debug.WriteLine($"DataAvailable() called");
                    float agg = Buffer.Aggregate((acc, x) => acc + x);
                    float avg = agg / Buffer.Length;
                    Debug.WriteLine($"Buffer average noise: {avg:0.###############}");

                    if (avg < 0.000001 && avg > -0.000001)
                    {
                        Debug.WriteLine($"Think it's dead air: {avg:0.###############}");
                    }
                    else
                    {
                        Debug.WriteLine("Calling sampleProvider.Write()");
                        for (int i = 0; i < Buffer.Length; i++) { Buffer[i] = Buffer[i]; }
                        //waveFileWriter?.Write(Buffer, Length);
                        sampleProvider.Write(Buffer, Length);
                    }
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
                            await Task.Delay(100, cancellationToken);

                        //start reading in the audio to analyze
                        analysis.ReadChunk(sampleProvider);

                        //start looking for landmarks in the audio once there are enough stripes
                        if (analysis.StripeCount > 2 * LandmarkFinder.RADIUS_TIME)
                            finder.Find(analysis.StripeCount - LandmarkFinder.RADIUS_TIME - 1);

                        //once there is enough audio processed send to Shazam
                        if (analysis.ProcessedMs >= retryMs)
                        {
                            _logger.LogInformation($"analysis.ProcessedMs: {analysis.ProcessedMs}");

                            //new Painter(analysis, finder).Paint(Path.Combine(outFolder, $"{DateTime.Now.ToString("yyyy-MM-dd-HH-mm-ss")}-spectro-bass.png"));

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
                    audioRecorder.Stop();
                }
            }
        }
    }
}