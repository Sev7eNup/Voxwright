using WriteSpeech.Core.Models;

namespace WriteSpeech.Core.Services.TextCorrection;

public interface ITextCorrectionService : IDisposable
{
    Task<string> CorrectAsync(string rawText, string? language, string? systemPromptOverride = null, CancellationToken ct = default);
    TextCorrectionProvider ProviderType { get; }
    bool IsModelLoaded { get; }
}
