using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Models;
using WriteSpeech.Core.Services.IDE;

namespace WriteSpeech.Core.Services.TextCorrection;

/// <summary>
/// Shared base class for all cloud-based text correction providers (OpenAI, Anthropic, Google, Groq).
/// Handles system prompt assembly (dictionary, IDE context, vocab extraction) and response processing.
/// </summary>
public abstract class CloudTextCorrectionServiceBase : ITextCorrectionService
{
    protected readonly ILogger Logger;
    protected readonly IOptionsMonitor<WriteSpeechOptions> OptionsMonitor;
    protected readonly IDictionaryService DictionaryService;
    protected readonly IIDEContextService IdeContextService;

    public abstract TextCorrectionProvider ProviderType { get; }
    public virtual bool IsModelLoaded => true;

    protected CloudTextCorrectionServiceBase(
        ILogger logger,
        IOptionsMonitor<WriteSpeechOptions> optionsMonitor,
        IDictionaryService dictionaryService,
        IIDEContextService ideContextService)
    {
        Logger = logger;
        OptionsMonitor = optionsMonitor;
        DictionaryService = dictionaryService;
        IdeContextService = ideContextService;
    }

    public async Task<string> CorrectAsync(
        string rawText,
        string? language,
        string? systemPromptOverride = null,
        string? targetLanguage = null,
        CancellationToken ct = default)
    {
        try
        {
            var options = OptionsMonitor.CurrentValue;
            var (systemPrompt, userMessage) = BuildPrompt(options, rawText, language, systemPromptOverride, targetLanguage);

            Logger.LogInformation("Sending text correction request ({Length} chars, provider: {Provider})",
                rawText.Length, ProviderType);

            var correctedText = await SendCorrectionRequestAsync(systemPrompt, userMessage, ct);

            Logger.LogInformation("Text correction completed: {OrigLength} → {CorrLength} chars",
                rawText.Length, correctedText?.Length ?? 0);

            return ProcessResponse(correctedText, rawText, options.TextCorrection.AutoAddToDictionary);
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Text correction failed ({Provider}), returning raw text", ProviderType);
            return rawText;
        }
    }

    protected abstract Task<string?> SendCorrectionRequestAsync(
        string systemPrompt, string userMessage, CancellationToken ct);

    protected (string systemPrompt, string userMessage) BuildPrompt(
        WriteSpeechOptions options,
        string rawText,
        string? language,
        string? systemPromptOverride,
        string? targetLanguage)
    {
        var systemPrompt = systemPromptOverride
            ?? options.TextCorrection.SystemPrompt
            ?? TextCorrectionDefaults.CorrectionSystemPrompt;

        systemPrompt += DictionaryService.BuildPromptFragment();
        systemPrompt += IdeContextService.BuildPromptFragment();

        if (options.TextCorrection.AutoAddToDictionary)
            systemPrompt += TextCorrectionDefaults.VocabExtractionInstruction;

        string userMessage;
        if (!string.IsNullOrEmpty(targetLanguage))
        {
            userMessage = $"[Translate to: {targetLanguage}]\n{rawText}";
        }
        else if (systemPromptOverride is not null)
        {
            userMessage = rawText;
        }
        else
        {
            var languageHint = string.IsNullOrEmpty(language)
                ? "Keep the SAME language as the input — do NOT translate"
                : $"Output language MUST be: {language}";
            userMessage = $"[{languageHint}]\n{rawText}";
        }

        return (systemPrompt, userMessage);
    }

    protected string ProcessResponse(string? correctedText, string rawText, bool autoAddToDictionary)
    {
        if (string.IsNullOrWhiteSpace(correctedText))
            return rawText;

        if (autoAddToDictionary)
        {
            var (cleanText, vocab) = VocabResponseParser.Parse(correctedText);
            VocabResponseParser.AddExtractedVocabulary(vocab, DictionaryService, Logger);
            return string.IsNullOrWhiteSpace(cleanText) ? rawText : cleanText;
        }

        return correctedText;
    }

    public virtual void Dispose()
    {
    }
}
