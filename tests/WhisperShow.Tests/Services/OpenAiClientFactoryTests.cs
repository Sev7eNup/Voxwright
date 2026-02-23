using FluentAssertions;
using WhisperShow.Core.Configuration;
using WhisperShow.Core.Services;
using WhisperShow.Tests.TestHelpers;

namespace WhisperShow.Tests.Services;

public class OpenAiClientFactoryTests
{
    private const string TestApiKey = "sk-test-key-12345678901234567890123456789012345678901234567890";

    [Fact]
    public void GetClient_ReturnsClient_WhenApiKeyConfigured()
    {
        var monitor = OptionsHelper.CreateMonitor(o => o.OpenAI.ApiKey = TestApiKey);
        var factory = new OpenAiClientFactory(monitor);

        var client = factory.GetClient();

        client.Should().NotBeNull();
    }

    [Fact]
    public void GetClient_ReturnsSameClient_WhenOptionsUnchanged()
    {
        var monitor = OptionsHelper.CreateMonitor(o => o.OpenAI.ApiKey = TestApiKey);
        var factory = new OpenAiClientFactory(monitor);

        var first = factory.GetClient();
        var second = factory.GetClient();

        ReferenceEquals(first, second).Should().BeTrue();
    }

    [Fact]
    public void GetClient_CreatesNewClient_WhenApiKeyChanges()
    {
        var options = new WhisperShowOptions { OpenAI = { ApiKey = TestApiKey } };
        var monitor = new TestOptionsMonitor<WhisperShowOptions>(options);
        var factory = new OpenAiClientFactory(monitor);

        var first = factory.GetClient();

        var updated = new WhisperShowOptions { OpenAI = { ApiKey = TestApiKey + "-changed" } };
        monitor.Update(updated);

        var second = factory.GetClient();

        ReferenceEquals(first, second).Should().BeFalse();
    }

    [Fact]
    public void GetClient_CreatesNewClient_WhenEndpointChanges()
    {
        var options = new WhisperShowOptions
        {
            OpenAI = { ApiKey = TestApiKey, Endpoint = "https://api.example.com/v1" }
        };
        var monitor = new TestOptionsMonitor<WhisperShowOptions>(options);
        var factory = new OpenAiClientFactory(monitor);

        var first = factory.GetClient();

        var updated = new WhisperShowOptions
        {
            OpenAI = { ApiKey = TestApiKey, Endpoint = "https://api.other.com/v1" }
        };
        monitor.Update(updated);

        var second = factory.GetClient();

        ReferenceEquals(first, second).Should().BeFalse();
    }

    [Fact]
    public void GetAudioClient_ReturnsNonNull()
    {
        var monitor = OptionsHelper.CreateMonitor(o => o.OpenAI.ApiKey = TestApiKey);
        var factory = new OpenAiClientFactory(monitor);

        var audioClient = factory.GetAudioClient("whisper-1");

        audioClient.Should().NotBeNull();
    }

    [Fact]
    public void GetChatClient_ReturnsNonNull()
    {
        var monitor = OptionsHelper.CreateMonitor(o => o.OpenAI.ApiKey = TestApiKey);
        var factory = new OpenAiClientFactory(monitor);

        var chatClient = factory.GetChatClient("gpt-4o-mini");

        chatClient.Should().NotBeNull();
    }
}
