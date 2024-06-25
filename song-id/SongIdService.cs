using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace song_id
{
    public class SongIdService : BackgroundService
    {
        private readonly ILogger<SongIdService> _logger;
        private readonly SongId _songId;

        public ShazamResult NowPlaying { get; private set; } = new ShazamResult { Title = "Dead Air" };

        public Action? SongChanged { get; set; }

        public SongIdService(ILogger<SongIdService> logger, IOptions<SongIdServiceOptions> options)
        {
            _logger = logger;
            //_songId = new SongId(new RecordingDevice(options.Value.RecordingDeviceIdx, "Configured Output Device"), logger);
            _songId = new SongId(new RecordingDevice(options.Value.RecordingDeviceName), logger);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10_000, stoppingToken);
                
                var result = await _songId.CaptureAndTagAsync(stoppingToken);

                if (result.Success && result.Title != NowPlaying.Title) _logger.LogInformation($"New track! {result}");
                else if(!result.Success) _logger.LogError($"Failed Shazam request {result}");

                NowPlaying = result;
                SongChanged?.Invoke();
            }
        }
    }
}
