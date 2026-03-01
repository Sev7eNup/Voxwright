namespace WriteSpeech.Core.Models;

public class TranscriptionHistoryEntry
{
    public string Text { get; set; } = "";
    public DateTime TimestampUtc { get; set; } = DateTime.UtcNow;
    public string Provider { get; set; } = "";
    public double DurationSeconds { get; set; }
    public string? SourceFilePath { get; set; }

    public string Preview => Text.Length > 80 ? Text[..80] + "..." : Text;

    public string TimeAgo
    {
        get
        {
            var diff = DateTime.UtcNow - TimestampUtc;
            return diff.TotalMinutes < 1 ? "just now"
                : diff.TotalMinutes < 60 ? $"{(int)diff.TotalMinutes}m ago"
                : diff.TotalHours < 24 ? $"{(int)diff.TotalHours}h ago"
                : $"{(int)diff.TotalDays}d ago";
        }
    }
}
