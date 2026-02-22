namespace WhisperShow.Core.Services.Hotkey;

public interface IGlobalHotkeyService : IDisposable
{
    event EventHandler? ToggleHotkeyPressed;
    event EventHandler? PushToTalkHotkeyPressed;
    event EventHandler? PushToTalkHotkeyReleased;
    event EventHandler? EscapePressed;
    void Register(IntPtr windowHandle);
    void Unregister();
    void UpdateToggleHotkey(string modifiers, string key);
    void UpdatePushToTalkHotkey(string modifiers, string key);
    void RegisterEscapeHotkey();
    void UnregisterEscapeHotkey();
}
