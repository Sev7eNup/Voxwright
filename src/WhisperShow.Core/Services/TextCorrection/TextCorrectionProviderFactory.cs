using WhisperShow.Core.Models;

namespace WhisperShow.Core.Services.TextCorrection;

public class TextCorrectionProviderFactory
{
    private readonly IEnumerable<ITextCorrectionService> _providers;

    public TextCorrectionProviderFactory(IEnumerable<ITextCorrectionService> providers)
    {
        _providers = providers;
    }

    public virtual ITextCorrectionService? GetProvider(TextCorrectionProvider provider)
    {
        return provider switch
        {
            TextCorrectionProvider.Cloud => _providers.OfType<OpenAiTextCorrectionService>().FirstOrDefault(),
            TextCorrectionProvider.Local => _providers.OfType<LocalTextCorrectionService>().FirstOrDefault(),
            _ => null
        };
    }
}
