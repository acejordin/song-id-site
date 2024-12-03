using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Net.Http.Headers;
using System.Web;

namespace song_id
{
    internal class IceCast
    {
        private ILogger<SongIdService> _logger;
        private SongIdServiceOptions _options;
        private SongIdServiceIceCastSecrets _iceCastSecrets;

        private static string streamUrlStem = $"/{_mount}";
        private static string updateMetadataUrlStem = "/admin/metadata?mount=/{0}&mode=updinfo&song={1}";
        private static string _mount = "vinyl.mp3";
        private static HttpClient _httpClient = new HttpClient();

        public IceCast(ILogger<SongIdService> logger, IOptions<SongIdServiceOptions> options, IOptions<SongIdServiceIceCastSecrets> iceCastSecrets)
        {
            _logger = logger;
            _options = options.Value;
            _iceCastSecrets = iceCastSecrets.Value;

            Uri baseUri = new Uri(_options.IceCastURL);
            _httpClient.BaseAddress = baseUri;
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.ConnectionClose = true;
        }

        public void OpenStation()
        {

        }

        internal async void UpdateIceCastMetadata(ShazamResult shazamResult)
        {
            string songString = HttpUtility.UrlEncode($"{shazamResult.Title} - {shazamResult.Artist}");
            var authenticationString = $"{_iceCastSecrets.User}:{_iceCastSecrets.Password}";
            var base64EncodedAuthenticationString = Convert.ToBase64String(System.Text.ASCIIEncoding.ASCII.GetBytes(authenticationString));

            HttpRequestMessage httpRequest = new HttpRequestMessage(HttpMethod.Get, string.Empty);
            httpRequest.RequestUri = new Uri(string.Format(updateMetadataUrlStem, _mount, songString), UriKind.Relative);
            httpRequest.Headers.Authorization = new AuthenticationHeaderValue("Basic", base64EncodedAuthenticationString);

            HttpResponseMessage response = await _httpClient.SendAsync(httpRequest);
            Debug.WriteLine($"Sent IceCast metadata request - StatusCode: {response.StatusCode}");
        }
    }
}
