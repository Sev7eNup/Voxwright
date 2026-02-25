using System.Runtime.InteropServices;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Services.Hotkey;

namespace WriteSpeech.App.Services;

public class LowLevelHookHotkeyService : IGlobalHotkeyService
{
    private readonly ILogger<LowLevelHookHotkeyService> _logger;
    private HotkeyBinding _toggleBinding;
    private HotkeyBinding _pttBinding;
    private bool _escapeRegistered;
    private bool _isPttActive;
    private bool _disposed;

    private IntPtr _keyboardHookHandle;
    private IntPtr _mouseHookHandle;

    // Must hold references to prevent GC collection of delegates passed to unmanaged code
    private readonly NativeMethods.LowLevelHookProc _keyboardHookDelegate;
    private readonly NativeMethods.LowLevelHookProc _mouseHookDelegate;

    public event EventHandler? ToggleHotkeyPressed;
    public event EventHandler? PushToTalkHotkeyPressed;
    public event EventHandler? PushToTalkHotkeyReleased;
    public event EventHandler? EscapePressed;

    public LowLevelHookHotkeyService(
        ILogger<LowLevelHookHotkeyService> logger,
        IOptionsMonitor<WriteSpeechOptions> optionsMonitor)
    {
        _logger = logger;
        _toggleBinding = optionsMonitor.CurrentValue.Hotkey.Toggle;
        _pttBinding = optionsMonitor.CurrentValue.Hotkey.PushToTalk;

        _keyboardHookDelegate = KeyboardHookCallback;
        _mouseHookDelegate = MouseHookCallback;
    }

    public void Register(IntPtr windowHandle)
    {
        var moduleHandle = NativeMethods.GetModuleHandle(null);

        _keyboardHookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_KEYBOARD_LL, _keyboardHookDelegate, moduleHandle, 0);

        if (_keyboardHookHandle == IntPtr.Zero)
            _logger.LogWarning("Failed to install WH_KEYBOARD_LL hook. Error: {Error}",
                Marshal.GetLastWin32Error());

        InstallMouseHookIfNeeded(moduleHandle);

        _logger.LogInformation("Low-level hooks installed (KB: {KB}, Mouse: {Mouse})",
            _keyboardHookHandle != IntPtr.Zero,
            _mouseHookHandle != IntPtr.Zero);
    }

    public void Unregister()
    {
        if (_keyboardHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_keyboardHookHandle);
            _keyboardHookHandle = IntPtr.Zero;
        }

        RemoveMouseHook();
        _logger.LogInformation("Low-level hooks removed");
    }

    public void RegisterEscapeHotkey() => _escapeRegistered = true;

    public void UnregisterEscapeHotkey() => _escapeRegistered = false;

    public void UpdateToggleHotkey(string modifiers, string key)
    {
        _toggleBinding = new HotkeyBinding { Modifiers = modifiers, Key = key };
        ReinstallMouseHookIfNeeded();
        _logger.LogInformation("Toggle hotkey updated to {Modifiers}+{Key}", modifiers, key);
    }

    public void UpdateToggleHotkey(string modifiers, string? key, string? mouseButton)
    {
        _toggleBinding = new HotkeyBinding { Modifiers = modifiers, Key = key ?? "", MouseButton = mouseButton };
        ReinstallMouseHookIfNeeded();
        _logger.LogInformation("Toggle hotkey updated to {Modifiers}+{Key}/{Mouse}", modifiers, key, mouseButton);
    }

    public void UpdatePushToTalkHotkey(string modifiers, string key)
    {
        _pttBinding = new HotkeyBinding { Modifiers = modifiers, Key = key };
        ReinstallMouseHookIfNeeded();
        _logger.LogInformation("PTT hotkey updated to {Modifiers}+{Key}", modifiers, key);
    }

    public void UpdatePushToTalkHotkey(string modifiers, string? key, string? mouseButton)
    {
        _pttBinding = new HotkeyBinding { Modifiers = modifiers, Key = key ?? "", MouseButton = mouseButton };
        ReinstallMouseHookIfNeeded();
        _logger.LogInformation("PTT hotkey updated to {Modifiers}+{Key}/{Mouse}", modifiers, key, mouseButton);
    }

    private IntPtr KeyboardHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.KBDLLHOOKSTRUCT>(lParam);

            // Ignore injected input to prevent loops with SendInput
            if ((hookStruct.flags & NativeMethods.LLKHF_INJECTED) != 0)
                return NativeMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);

            var msg = wParam.ToInt32();
            var isDown = msg is NativeMethods.WM_KEYDOWN or NativeMethods.WM_SYSKEYDOWN;
            var isUp = msg is NativeMethods.WM_KEYUP or NativeMethods.WM_SYSKEYUP;

            if (isDown && hookStruct.vkCode == (uint)NativeMethods.VK_ESCAPE && _escapeRegistered)
            {
                EscapePressed?.Invoke(this, EventArgs.Empty);
            }

            // Toggle hotkey (keyboard-based)
            if (isDown && !_toggleBinding.IsMouseBinding
                && HotkeyMatcher.MatchesKeyboardBinding(_toggleBinding, hookStruct.vkCode, NativeMethods.GetAsyncKeyState))
            {
                _logger.LogInformation("Toggle hotkey pressed (LL hook)");
                ToggleHotkeyPressed?.Invoke(this, EventArgs.Empty);
            }

            // PTT hotkey (keyboard-based)
            if (!_pttBinding.IsMouseBinding)
            {
                if (isDown && !_isPttActive
                    && HotkeyMatcher.MatchesKeyboardBinding(_pttBinding, hookStruct.vkCode, NativeMethods.GetAsyncKeyState))
                {
                    _isPttActive = true;
                    _logger.LogInformation("PTT pressed (LL hook)");
                    PushToTalkHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }
                else if (isUp && _isPttActive && HotkeyMatcher.MatchesKeyRelease(_pttBinding, hookStruct.vkCode))
                {
                    _isPttActive = false;
                    _logger.LogInformation("PTT released (LL hook)");
                    PushToTalkHotkeyReleased?.Invoke(this, EventArgs.Empty);
                }
            }
        }

        return NativeMethods.CallNextHookEx(_keyboardHookHandle, nCode, wParam, lParam);
    }

    private IntPtr MouseHookCallback(int nCode, IntPtr wParam, IntPtr lParam)
    {
        if (nCode >= 0)
        {
            var hookStruct = Marshal.PtrToStructure<NativeMethods.MSLLHOOKSTRUCT>(lParam);
            var msg = wParam.ToInt32();
            var (button, isDown) = HotkeyMatcher.ClassifyMouseMessage(msg, hookStruct.mouseData);

            if (button != null)
            {
                // Toggle hotkey (mouse-based)
                if (isDown && _toggleBinding.IsMouseBinding
                    && HotkeyMatcher.MatchesMouseBinding(_toggleBinding, button, NativeMethods.GetAsyncKeyState))
                {
                    _logger.LogInformation("Toggle hotkey pressed via mouse (LL hook)");
                    ToggleHotkeyPressed?.Invoke(this, EventArgs.Empty);
                }

                // PTT hotkey (mouse-based)
                if (_pttBinding.IsMouseBinding)
                {
                    if (isDown && !_isPttActive
                        && HotkeyMatcher.MatchesMouseBinding(_pttBinding, button, NativeMethods.GetAsyncKeyState))
                    {
                        _isPttActive = true;
                        _logger.LogInformation("PTT pressed via mouse (LL hook)");
                        PushToTalkHotkeyPressed?.Invoke(this, EventArgs.Empty);
                    }
                    else if (!isDown && _isPttActive
                        && string.Equals(_pttBinding.MouseButton, button, StringComparison.OrdinalIgnoreCase))
                    {
                        _isPttActive = false;
                        _logger.LogInformation("PTT released via mouse (LL hook)");
                        PushToTalkHotkeyReleased?.Invoke(this, EventArgs.Empty);
                    }
                }
            }
        }

        return NativeMethods.CallNextHookEx(_mouseHookHandle, nCode, wParam, lParam);
    }

    private bool NeedsMouseHook() => _toggleBinding.IsMouseBinding || _pttBinding.IsMouseBinding;

    private void InstallMouseHookIfNeeded(IntPtr moduleHandle)
    {
        if (!NeedsMouseHook() || _mouseHookHandle != IntPtr.Zero) return;

        _mouseHookHandle = NativeMethods.SetWindowsHookExW(
            NativeMethods.WH_MOUSE_LL, _mouseHookDelegate, moduleHandle, 0);

        if (_mouseHookHandle == IntPtr.Zero)
            _logger.LogWarning("Failed to install WH_MOUSE_LL hook. Error: {Error}",
                Marshal.GetLastWin32Error());
    }

    private void RemoveMouseHook()
    {
        if (_mouseHookHandle != IntPtr.Zero)
        {
            NativeMethods.UnhookWindowsHookEx(_mouseHookHandle);
            _mouseHookHandle = IntPtr.Zero;
        }
    }

    private void ReinstallMouseHookIfNeeded()
    {
        if (NeedsMouseHook() && _mouseHookHandle == IntPtr.Zero && _keyboardHookHandle != IntPtr.Zero)
        {
            var moduleHandle = NativeMethods.GetModuleHandle(null);
            InstallMouseHookIfNeeded(moduleHandle);
        }
        else if (!NeedsMouseHook() && _mouseHookHandle != IntPtr.Zero)
        {
            RemoveMouseHook();
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        Unregister();
    }
}
