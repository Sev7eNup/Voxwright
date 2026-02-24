namespace WriteSpeech.Core.Services.TextInsertion;

public interface IWindowFocusService
{
    IntPtr GetForegroundWindow();
    Task RestoreFocusAsync(IntPtr windowHandle);
    string? GetProcessName(IntPtr windowHandle);
}
