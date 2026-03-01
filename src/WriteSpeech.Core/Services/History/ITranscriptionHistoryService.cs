using WriteSpeech.Core.Models;

namespace WriteSpeech.Core.Services.History;

public interface ITranscriptionHistoryService : IDisposable
{
    IReadOnlyList<TranscriptionHistoryEntry> GetEntries();
    void AddEntry(string text, string provider, double durationSeconds, string? sourceFilePath = null);
    void RemoveEntry(TranscriptionHistoryEntry entry);
    void Clear();
    Task LoadAsync();
    Task SaveAsync();
}
