using FluentAssertions;
using NSubstitute;
using WriteSpeech.Core.Models;
using WriteSpeech.Core.Services.TextCorrection;

namespace WriteSpeech.Tests.Services;

public class TextCorrectionProviderFactoryTests
{
    private readonly ITextCorrectionService _openAiService;
    private readonly ITextCorrectionService _anthropicService;
    private readonly ITextCorrectionService _googleService;
    private readonly ITextCorrectionService _groqService;
    private readonly ITextCorrectionService _localService;
    private readonly TextCorrectionProviderFactory _factory;

    public TextCorrectionProviderFactoryTests()
    {
        _openAiService = Substitute.For<ITextCorrectionService>();
        _openAiService.ProviderType.Returns(TextCorrectionProvider.OpenAI);

        _anthropicService = Substitute.For<ITextCorrectionService>();
        _anthropicService.ProviderType.Returns(TextCorrectionProvider.Anthropic);

        _googleService = Substitute.For<ITextCorrectionService>();
        _googleService.ProviderType.Returns(TextCorrectionProvider.Google);

        _groqService = Substitute.For<ITextCorrectionService>();
        _groqService.ProviderType.Returns(TextCorrectionProvider.Groq);

        _localService = Substitute.For<ITextCorrectionService>();
        _localService.ProviderType.Returns(TextCorrectionProvider.Local);

        _factory = new TextCorrectionProviderFactory(
            [_openAiService, _anthropicService, _googleService, _groqService, _localService]);
    }

    [Fact]
    public void GetProvider_OpenAI_ReturnsOpenAiService()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.OpenAI);
        provider.Should().BeSameAs(_openAiService);
    }

    [Fact]
    public void GetProvider_Cloud_MapsToOpenAI()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Cloud);
        provider.Should().BeSameAs(_openAiService);
    }

    [Fact]
    public void GetProvider_Anthropic_ReturnsAnthropicService()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Anthropic);
        provider.Should().BeSameAs(_anthropicService);
    }

    [Fact]
    public void GetProvider_Google_ReturnsGoogleService()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Google);
        provider.Should().BeSameAs(_googleService);
    }

    [Fact]
    public void GetProvider_Groq_ReturnsGroqService()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Groq);
        provider.Should().BeSameAs(_groqService);
    }

    [Fact]
    public void GetProvider_Local_ReturnsLocalService()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Local);
        provider.Should().BeSameAs(_localService);
    }

    [Fact]
    public void GetProvider_Off_ReturnsNull()
    {
        var provider = _factory.GetProvider(TextCorrectionProvider.Off);
        provider.Should().BeNull();
    }

    [Fact]
    public void GetProvider_UnknownProvider_ThrowsArgumentOutOfRange()
    {
        var act = () => _factory.GetProvider((TextCorrectionProvider)999);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
