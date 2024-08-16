namespace song_id
{
    public class SongIdServiceOptions
    {
        public string RecordingDeviceName { get; set; } = string.Empty;
        public int DeadAirLengthSecs { get; set; } = 10;
        public string IceCastURL { get; set; } = string.Empty;
    }
}
