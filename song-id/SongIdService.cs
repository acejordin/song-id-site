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

        public SongIdService(ILogger<SongIdService> logger, IOptions<SongIdServiceOptions> options)
        {
            _logger = logger;
            _songId = new SongId(new RecordingDevice(options.Value.RecordingDeviceIdx, "Device"));
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //NowPlaying = new ShazamResult
            //{
            //    Artist = "Thrice",
            //    Title = "Artist in the Ambulance",
            //    Success = true,
            //    ImageUrl = @"https://is4-ssl.mzstatic.com/image/thumb/Music116/v4/f5/cc/55/f5cc5594-0ab4-645f-d61a-c6d79d6b8a33/06UMGIM09833.rgb.jpg/400x400cc.jpg",
            //    Url = "https://www.shazam.com/track/20115818/the-artist-in-the-ambulance"
            //};
            while (!stoppingToken.IsCancellationRequested)
            {
                await Task.Delay(10_000, stoppingToken);
                
                var result = await _songId.CaptureAndTagAsync(stoppingToken);

                if (result.Success && result.Title != NowPlaying.Title) _logger.LogInformation($"New track! {result}");
                else if(!result.Success) _logger.LogError($"Failed Shazam request {result}");

                NowPlaying = result;
            }
        }
    }
}
