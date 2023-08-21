using Microsoft.AspNetCore.Mvc;
using song_id;

namespace song_id_site.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class NowPlayingController : ControllerBase
    {
        private readonly SongIdService _songIdService;
        private readonly ILogger _logger;
        public NowPlayingController(SongIdService songIdService, ILogger<NowPlayingController> logger)
        {
            _songIdService = songIdService;
            _logger = logger;
        }

        [HttpGet]
        public ShazamResult Get()
        {
            _logger.LogInformation($"NowPlaying: {_songIdService.NowPlaying}");
            return _songIdService.NowPlaying;
        }
    }
}
