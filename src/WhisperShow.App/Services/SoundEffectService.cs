using System.Media;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WhisperShow.Core.Configuration;
using WhisperShow.Core.Services.Audio;

namespace WhisperShow.App.Services;

public class SoundEffectService : ISoundEffectService
{
    private readonly ILogger<SoundEffectService> _logger;
    private readonly IOptionsMonitor<WhisperShowOptions> _optionsMonitor;

    private bool Enabled => _optionsMonitor.CurrentValue.App.SoundEffects;

    public SoundEffectService(ILogger<SoundEffectService> logger,
                              IOptionsMonitor<WhisperShowOptions> optionsMonitor)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
    }

    public void PlayStartRecording()
    {
        if (!Enabled) return;
        _logger.LogDebug("Playing start recording sound");
        SystemSounds.Exclamation.Play();
    }

    public void PlayStopRecording()
    {
        if (!Enabled) return;
        _logger.LogDebug("Playing stop recording sound");
        SystemSounds.Asterisk.Play();
    }

    public void PlayError()
    {
        if (!Enabled) return;
        _logger.LogDebug("Playing error sound");
        SystemSounds.Hand.Play();
    }
}
