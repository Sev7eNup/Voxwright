namespace WriteSpeech.Core.Services.Audio;

public interface IAudioRecordingService : IDisposable
{
    event EventHandler<float>? AudioLevelChanged;
    event EventHandler<Exception>? RecordingError;
    event EventHandler? MaxDurationReached;
    Task StartRecordingAsync();
    Task<byte[]> StopRecordingAsync();
    bool IsRecording { get; }
}
