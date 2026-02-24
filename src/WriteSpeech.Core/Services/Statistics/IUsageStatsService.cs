using WriteSpeech.Core.Models;

namespace WriteSpeech.Core.Services.Statistics;

public interface IUsageStatsService : IDisposable
{
    UsageStats GetStats();
    void RecordTranscription(double durationSeconds, long audioBytesProcessed, string provider, int wordCount, string correctionProvider);
    void RecordError();
    Task SaveAsync();
    Task LoadAsync();
    void Reset();
}
