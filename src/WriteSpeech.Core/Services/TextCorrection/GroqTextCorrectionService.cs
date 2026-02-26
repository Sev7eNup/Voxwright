using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Models;
using WriteSpeech.Core.Services.IDE;

namespace WriteSpeech.Core.Services.TextCorrection;

public class GroqTextCorrectionService : CloudTextCorrectionServiceBase
{
    public override TextCorrectionProvider ProviderType => TextCorrectionProvider.Groq;

    public GroqTextCorrectionService(
        ILogger<GroqTextCorrectionService> logger,
        IOptionsMonitor<WriteSpeechOptions> optionsMonitor,
        IDictionaryService dictionaryService,
        IIDEContextService ideContextService)
        : base(logger, optionsMonitor, dictionaryService, ideContextService)
    {
    }

    protected override async Task<string?> SendCorrectionRequestAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        var groq = OptionsMonitor.CurrentValue.TextCorrection.Groq;

        if (string.IsNullOrWhiteSpace(groq.ApiKey))
        {
            Logger.LogWarning("Groq API key not configured, skipping text correction");
            return null;
        }

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(groq.Endpoint))
            clientOptions.Endpoint = new Uri(groq.Endpoint);

        var client = new OpenAIClient(
            credential: new ApiKeyCredential(groq.ApiKey),
            options: clientOptions);

        var chatClient = client.GetChatClient(groq.Model);

        var result = await chatClient.CompleteChatAsync(
            [
                new SystemChatMessage(systemPrompt),
                new UserChatMessage(userMessage)
            ],
            new ChatCompletionOptions { Temperature = 0 },
            ct);

        return result.Value.Content.FirstOrDefault()?.Text;
    }
}
