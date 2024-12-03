using ManagedBass;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using song_id;

namespace song_id_test
{
    [TestClass]
    public class SongIdTests
    {
        [TestMethod]
        public async Task CaptureAndTagTest_Success()
        {
            //Start playing a song before running test
            //Check that you are passing correct recording device to SongId
            SongId songId = new SongId(new NetRadio("https://live.ukrp.tv/outreachradio.mp3"), NullLogger<SongIdTests>.Instance, 2);

            ShazamResult result = await songId.CaptureAndTagAsync(CancellationToken.None, new RecordingSourceChangedToken());

            Assert.AreEqual(true, result.Success);
        }
    }
}