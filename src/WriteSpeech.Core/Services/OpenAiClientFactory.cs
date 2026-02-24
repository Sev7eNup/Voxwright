using System.ClientModel;
using Microsoft.Extensions.Options;
using OpenAI;
using OpenAI.Audio;
using OpenAI.Chat;
using WriteSpeech.Core.Configuration;

namespace WriteSpeech.Core.Services;

public class OpenAiClientFactory
{
    private readonly IOptionsMonitor<WriteSpeechOptions> _optionsMonitor;
    private readonly Lock _lock = new();
    private OpenAIClient? _client;
    private string? _lastApiKey;
    private string? _lastEndpoint;

    public OpenAiClientFactory(IOptionsMonitor<WriteSpeechOptions> optionsMonitor)
    {
        _optionsMonitor = optionsMonitor;
    }

    public OpenAIClient GetClient()
    {
        lock (_lock)
        {
            var opts = _optionsMonitor.CurrentValue.OpenAI;

            if (string.IsNullOrWhiteSpace(opts.ApiKey))
                throw new InvalidOperationException("OpenAI API key is not configured.");

            if (_client is null || _lastApiKey != opts.ApiKey || _lastEndpoint != opts.Endpoint)
            {
                var clientOptions = new OpenAIClientOptions();
                if (!string.IsNullOrEmpty(opts.Endpoint))
                    clientOptions.Endpoint = new Uri(opts.Endpoint);

                _client = new OpenAIClient(
                    credential: new ApiKeyCredential(opts.ApiKey),
                    options: clientOptions);
                _lastApiKey = opts.ApiKey;
                _lastEndpoint = opts.Endpoint;
            }

            return _client;
        }
    }

    public AudioClient GetAudioClient(string model) => GetClient().GetAudioClient(model);

    public ChatClient GetChatClient(string model) => GetClient().GetChatClient(model);
}
