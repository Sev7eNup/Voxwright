namespace WriteSpeech.Core.Models;

public record CorrectionMode(
    string Name,
    string SystemPrompt,
    IReadOnlyList<string> AppPatterns,
    bool IsBuiltIn = false,
    string? TargetLanguage = null);
