using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace song_id
{
    public class SongIdService : BackgroundService
    {
        private readonly ILogger<SongIdService> _logger;
        private SongId _songId;

        public ShazamResult NowPlaying { get; private set; } = new ShazamResult { Title = "Dead Air" };

        public Action? SongChanged { get; set; }

        public SongId SongId { get { return _songId; } }

        public SongIdService(ILogger<SongIdService> logger, IOptions<SongIdServiceOptions> options)
        {
            _logger = logger;
            _songId = new SongId(new RecordingDevice(options.Value.RecordingDeviceName), logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                RecordingSourceChangedToken recordingSourceChangedToken = new RecordingSourceChangedToken();

                var result = await _songId.CaptureAndTagAsync(stoppingToken, recordingSourceChangedToken);

                if (result.Success && result.Title != NowPlaying.Title) _logger.LogInformation($"New track! {result}");
                else if(!result.Success) _logger.LogError($"Failed Shazam request {result}");

                NowPlaying = result;
                SongChanged?.Invoke();

                //if recording source has changed, then skip waiting to speed things up
                if (!recordingSourceChangedToken.IsRecordingSourceChanged)
                {
                    await Task.Delay(10_000, stoppingToken);
                }
            }
        }
    }
}
