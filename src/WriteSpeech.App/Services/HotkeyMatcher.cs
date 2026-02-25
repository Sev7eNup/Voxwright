using System.Windows.Input;
using WriteSpeech.Core.Configuration;

namespace WriteSpeech.App.Services;

internal static class HotkeyMatcher
{
    internal static (string? Button, bool IsDown) ClassifyMouseMessage(int msg, uint mouseData)
    {
        return msg switch
        {
            NativeMethods.WM_MBUTTONDOWN => ("Middle", true),
            NativeMethods.WM_MBUTTONUP => ("Middle", false),
            NativeMethods.WM_XBUTTONDOWN => (GetXButton(mouseData), true),
            NativeMethods.WM_XBUTTONUP => (GetXButton(mouseData), false),
            _ => (null, false)
        };
    }

    internal static string? GetXButton(uint mouseData)
    {
        var hiWord = (mouseData >> 16) & 0xFFFF;
        return hiWord switch
        {
            NativeMethods.XBUTTON1 => "XButton1",
            NativeMethods.XBUTTON2 => "XButton2",
            _ => null
        };
    }

    internal static bool MatchesKeyboardBinding(HotkeyBinding binding, uint vkCode, Func<int, short> getKeyState)
    {
        if (binding.IsMouseBinding) return false;

        if (!Enum.TryParse<Key>(binding.Key, true, out var key))
            return false;

        var expectedVk = (uint)KeyInterop.VirtualKeyFromKey(key);
        if (vkCode != expectedVk) return false;

        return AreModifiersPressed(binding.Modifiers, getKeyState);
    }

    internal static bool MatchesKeyRelease(HotkeyBinding binding, uint vkCode)
    {
        if (binding.IsMouseBinding) return false;

        if (!Enum.TryParse<Key>(binding.Key, true, out var key))
            return false;

        return vkCode == (uint)KeyInterop.VirtualKeyFromKey(key);
    }

    internal static bool MatchesMouseBinding(HotkeyBinding binding, string? button, Func<int, short> getKeyState)
    {
        if (!binding.IsMouseBinding || button == null) return false;
        if (!string.Equals(binding.MouseButton, button, StringComparison.OrdinalIgnoreCase)) return false;

        return AreModifiersPressed(binding.Modifiers, getKeyState);
    }

    internal static bool AreModifiersPressed(string modifiers, Func<int, short> getKeyState)
    {
        if (string.IsNullOrEmpty(modifiers)) return true;

        foreach (var part in modifiers.Split(',', StringSplitOptions.TrimEntries))
        {
            bool isDown = part switch
            {
                "Control" => IsKeyDown(getKeyState, NativeMethods.VK_LCONTROL)
                          || IsKeyDown(getKeyState, NativeMethods.VK_RCONTROL),
                "Shift" => IsKeyDown(getKeyState, NativeMethods.VK_LSHIFT)
                        || IsKeyDown(getKeyState, NativeMethods.VK_RSHIFT),
                "Alt" => IsKeyDown(getKeyState, NativeMethods.VK_LMENU)
                      || IsKeyDown(getKeyState, NativeMethods.VK_RMENU),
                _ => true
            };
            if (!isDown) return false;
        }
        return true;
    }

    private static bool IsKeyDown(Func<int, short> getKeyState, int vk)
        => (getKeyState(vk) & 0x8000) != 0;
}
