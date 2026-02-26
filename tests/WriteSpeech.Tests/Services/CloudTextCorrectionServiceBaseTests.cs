using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Models;
using WriteSpeech.Core.Services.IDE;
using WriteSpeech.Core.Services.TextCorrection;
using WriteSpeech.Tests.TestHelpers;

namespace WriteSpeech.Tests.Services;

public class CloudTextCorrectionServiceBaseTests
{
    private TestCorrectionService CreateService(Action<WriteSpeechOptions>? configure = null)
    {
        var options = OptionsHelper.CreateMonitor(o =>
        {
            o.TextCorrection.Provider = TextCorrectionProvider.OpenAI;
            configure?.Invoke(o);
        });

        var dictionaryService = Substitute.For<IDictionaryService>();
        dictionaryService.BuildPromptFragment().Returns("");

        var ideContextService = Substitute.For<IIDEContextService>();
        ideContextService.BuildPromptFragment().Returns("");

        return new TestCorrectionService(
            NullLogger<TestCorrectionService>.Instance,
            options, dictionaryService, ideContextService);
    }

    [Fact]
    public async Task CorrectAsync_ReturnsRawText_WhenSendReturnsNull()
    {
        var service = CreateService();
        service.ResponseToReturn = null;

        var result = await service.CorrectAsync("hello world", "en");

        result.Should().Be("hello world");
    }

    [Fact]
    public async Task CorrectAsync_ReturnsRawText_WhenSendReturnsEmpty()
    {
        var service = CreateService();
        service.ResponseToReturn = "   ";

        var result = await service.CorrectAsync("hello world", "en");

        result.Should().Be("hello world");
    }

    [Fact]
    public async Task CorrectAsync_ReturnsCorrectedText()
    {
        var service = CreateService();
        service.ResponseToReturn = "Hello, world!";

        var result = await service.CorrectAsync("hello world", "en");

        result.Should().Be("Hello, world!");
    }

    [Fact]
    public async Task CorrectAsync_ReturnsRawText_WhenExceptionThrown()
    {
        var service = CreateService();
        service.ShouldThrow = true;

        var result = await service.CorrectAsync("hello world", "en");

        result.Should().Be("hello world");
    }

    [Fact]
    public async Task CorrectAsync_IncludesLanguageHint_WhenLanguageProvided()
    {
        var service = CreateService();
        service.ResponseToReturn = "corrected";

        await service.CorrectAsync("test", "de");

        service.LastUserMessage.Should().Contain("Output language MUST be: de");
    }

    [Fact]
    public async Task CorrectAsync_IncludesNoTranslateHint_WhenNoLanguage()
    {
        var service = CreateService();
        service.ResponseToReturn = "corrected";

        await service.CorrectAsync("test", null);

        service.LastUserMessage.Should().Contain("do NOT translate");
    }

    [Fact]
    public async Task CorrectAsync_IncludesTranslateHint_WhenTargetLanguageProvided()
    {
        var service = CreateService();
        service.ResponseToReturn = "translated";

        await service.CorrectAsync("test", null, targetLanguage: "English");

        service.LastUserMessage.Should().Contain("[Translate to: English]");
    }

    [Fact]
    public async Task CorrectAsync_UsesDictionaryFragment()
    {
        var options = OptionsHelper.CreateMonitor();
        var dict = Substitute.For<IDictionaryService>();
        dict.BuildPromptFragment().Returns("\n[Custom: WriteSpeech]");
        var ide = Substitute.For<IIDEContextService>();
        ide.BuildPromptFragment().Returns("");

        var service = new TestCorrectionService(
            NullLogger<TestCorrectionService>.Instance, options, dict, ide);
        service.ResponseToReturn = "ok";

        await service.CorrectAsync("test", "en");

        service.LastSystemPrompt.Should().Contain("[Custom: WriteSpeech]");
    }

    [Fact]
    public async Task CorrectAsync_UsesSystemPromptOverride()
    {
        var service = CreateService();
        service.ResponseToReturn = "ok";

        await service.CorrectAsync("test", "en", systemPromptOverride: "Custom prompt");

        service.LastSystemPrompt.Should().StartWith("Custom prompt");
    }

    [Fact]
    public async Task CorrectAsync_ExtractsVocab_WhenAutoAddEnabled()
    {
        var options = OptionsHelper.CreateMonitor(o =>
            o.TextCorrection.AutoAddToDictionary = true);
        var dict = Substitute.For<IDictionaryService>();
        dict.BuildPromptFragment().Returns("");
        var ide = Substitute.For<IIDEContextService>();
        ide.BuildPromptFragment().Returns("");

        var service = new TestCorrectionService(
            NullLogger<TestCorrectionService>.Instance, options, dict, ide);
        service.ResponseToReturn = "Hello, world!\n---VOCAB---\nWriteSpeech";

        var result = await service.CorrectAsync("hello world", "en");

        result.Should().Be("Hello, world!");
        dict.Received().AddEntry("WriteSpeech");
    }

    [Fact]
    public void Dispose_DoesNotThrow()
    {
        var service = CreateService();
        var act = () => service.Dispose();
        act.Should().NotThrow();
    }

    private class TestCorrectionService : CloudTextCorrectionServiceBase
    {
        public string? ResponseToReturn { get; set; } = "corrected";
        public bool ShouldThrow { get; set; }
        public string? LastSystemPrompt { get; private set; }
        public string? LastUserMessage { get; private set; }

        public override TextCorrectionProvider ProviderType => TextCorrectionProvider.OpenAI;

        public TestCorrectionService(
            Microsoft.Extensions.Logging.ILogger logger,
            Microsoft.Extensions.Options.IOptionsMonitor<WriteSpeechOptions> optionsMonitor,
            IDictionaryService dictionaryService,
            IIDEContextService ideContextService)
            : base(logger, optionsMonitor, dictionaryService, ideContextService) { }

        protected override Task<string?> SendCorrectionRequestAsync(
            string systemPrompt, string userMessage, CancellationToken ct)
        {
            LastSystemPrompt = systemPrompt;
            LastUserMessage = userMessage;
            if (ShouldThrow)
                throw new System.Net.Http.HttpRequestException("test error");
            return Task.FromResult(ResponseToReturn);
        }
    }
}
