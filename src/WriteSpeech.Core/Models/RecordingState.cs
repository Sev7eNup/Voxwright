namespace WriteSpeech.Core.Models;

public enum RecordingState
{
    Idle,
    Recording,
    Transcribing,
    Result,
    Error
}
