namespace WhisperShow.Core.Services.Audio;

public interface ISoundEffectService
{
    void PlayStartRecording();
    void PlayStopRecording();
    void PlayError();
}
