namespace WriteSpeech.Core.Services.Audio;

public interface IAudioRecordingService : IDisposable
{
    event EventHandler<float>? AudioLevelChanged;
    Task StartRecordingAsync();
    Task<byte[]> StopRecordingAsync();
    bool IsRecording { get; }
}
