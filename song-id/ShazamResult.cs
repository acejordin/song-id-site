public class ShazamResult
{
    public bool Success { get; set; } = false;
    public string Artist { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public int RetryMs { get; set; } = 0;
    public string ImageUrl { get; set; } = string.Empty;
    public DateTime DetectedTime { get; } = DateTime.UtcNow;

    public override string ToString()
    {
        return Success ? $"{Title} - {Artist}" : "Nothing playing";
    }
}
