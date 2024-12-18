﻿using Microsoft.Extensions.Hosting;
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
            _songId = new SongId(new NetRadio("https://live.ukrp.tv/outreachradio.mp3"), logger, options.Value.DeadAirLengthSecs);
            _iceCast = new IceCast(logger, options, iceCastSecrets);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                var shazamResult = await _songId.CaptureAndTagAsync(stoppingToken);

                if (shazamResult.Success && shazamResult.Title != NowPlaying.Title) _logger.LogInformation($"New track! {shazamResult}");
                else if(!shazamResult.Success) _logger.LogError($"Failed Shazam request {shazamResult}");

                NowPlaying = shazamResult;
                SongChanged?.Invoke();
                //_iceCast.UpdateIceCastMetadata(shazamResult);
            }
        }
    }
}
