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
            SongId songId = new SongId(SongId.GetAvailableRecordingDevices()[7], NullLogger<SongIdTests>.Instance);

            ShazamResult result = await songId.CaptureAndTagAsync(CancellationToken.None);

            Assert.AreEqual(true, result.Success);
        }
    }
}