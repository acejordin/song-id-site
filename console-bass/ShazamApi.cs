﻿using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System.Text;

static class ShazamApi
{
    static private readonly HttpClient _httpClient = new HttpClient();
    static private readonly string INSTALLATION_ID = Guid.NewGuid().ToString();

    static ShazamApi()
    {
        _httpClient.DefaultRequestHeaders.UserAgent.ParseAdd("curl/7");
    }

    public static async Task<ShazamResult> SendRequest(string tagId, int samplems, byte[] sig)
    {
        var payload = new
        {
            signature = new
            {
                uri = "data:audio/vnd.shazam.sig;base64," + Convert.ToBase64String(sig),
                samplems
            }
        };

        var url = "https://amp.shazam.com/discovery/v5/en/US/android/-/tag/" + INSTALLATION_ID + "/" + tagId;
        var postData = new StringContent(
            JsonConvert.SerializeObject(payload),
            Encoding.UTF8,
            "application/json"
        );

        var result = new ShazamResult();

        var res = await _httpClient.PostAsync(url, postData);
        var obj = JsonConvert.DeserializeObject<JToken>(await res.Content.ReadAsStringAsync());
        var track = obj?.Value<JToken>("track");

        if (track != null)
        {
            result.Success = true;
            result.Url = track.Value<string>("url") ?? string.Empty;
            result.Title = track.Value<string>("title") ?? string.Empty;
            result.Artist = track.Value<string>("subtitle") ?? string.Empty;
            result.ImageUrl = track.Value<JToken>("images")?.Value<string>("coverart") ?? string.Empty;
        }
        else
        {
            result.RetryMs = obj?.Value<int>("retryms") ?? 0;
        }

        return result;
    }

}
