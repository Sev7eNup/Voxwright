using System.ClientModel;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Chat;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Models;
using WriteSpeech.Core.Services.IDE;

namespace WriteSpeech.Core.Services.TextCorrection;

public class GoogleTextCorrectionService : CloudTextCorrectionServiceBase
{
    public override TextCorrectionProvider ProviderType => TextCorrectionProvider.Google;

    public GoogleTextCorrectionService(
        ILogger<GoogleTextCorrectionService> logger,
        IOptionsMonitor<WriteSpeechOptions> optionsMonitor,
        IDictionaryService dictionaryService,
        IIDEContextService ideContextService)
        : base(logger, optionsMonitor, dictionaryService, ideContextService)
    {
    }

    protected override async Task<string?> SendCorrectionRequestAsync(
        string systemPrompt, string userMessage, CancellationToken ct)
    {
        var google = OptionsMonitor.CurrentValue.TextCorrection.Google;

        if (string.IsNullOrWhiteSpace(google.ApiKey))
        {
            Logger.LogWarning("Google API key not configured, skipping text correction");
            return null;
        }

        var clientOptions = new OpenAIClientOptions();
        if (!string.IsNullOrEmpty(google.Endpoint))
            clientOptions.Endpoint = new Uri(google.Endpoint);

        var client = new OpenAIClient(
            credential: new ApiKeyCredential(google.ApiKey),
            options: clientOptions);

        var chatClient = client.GetChatClient(google.Model);

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
