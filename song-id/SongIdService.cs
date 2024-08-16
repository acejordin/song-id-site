using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace song_id
{
    public class SongIdService : BackgroundService
    {
        private readonly ILogger<SongIdService> _logger;
        private SongId _songId;
        private IceCast _iceCast;

        public ShazamResult NowPlaying { get; private set; } = new ShazamResult { Title = "Dead Air" };

        public Action? SongChanged { get; set; }

        public SongId SongId { get { return _songId; } }

        public SongIdService(ILogger<SongIdService> logger, IOptions<SongIdServiceOptions> options, IOptions<SongIdServiceIceCastSecrets> iceCastSecrets)
        {
            _logger = logger;
            _songId = new SongId(new RecordingDevice(options.Value.RecordingDeviceName), logger, options.Value.DeadAirLengthSecs);
            _iceCast = new IceCast(logger, options, iceCastSecrets);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                RecordingSourceChangedToken recordingSourceChangedToken = new RecordingSourceChangedToken();

                var shazamResult = await _songId.CaptureAndTagAsync(stoppingToken, recordingSourceChangedToken);

                if (shazamResult.Success && shazamResult.Title != NowPlaying.Title) _logger.LogInformation($"New track! {shazamResult}");
                else if(!shazamResult.Success) _logger.LogError($"Failed Shazam request {shazamResult}");

                NowPlaying = shazamResult;
                SongChanged?.Invoke();
                _iceCast.UpdateIceCastMetadata(shazamResult);

                //if recording source has changed, then skip waiting to speed things up
                if (!recordingSourceChangedToken.IsRecordingSourceChanged)
                {
                    await Task.Delay(10_000, stoppingToken);
                }
            }
        }
    }
}
