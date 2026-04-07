using System.Runtime.InteropServices;
using System.Windows;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Voxwright.Core.Configuration;
using Voxwright.Core.Services.TextInsertion;

namespace Voxwright.App.Services;

/// <summary>
/// Inserts text at the current cursor position in any Windows application by leveraging
/// the system clipboard and simulated keystrokes.
///
/// Implementation approach:
/// 1. Saves the current clipboard contents (so the user's clipboard is not destroyed).
/// 2. Places the transcribed text onto the clipboard via <see cref="Clipboard.SetText"/>.
/// 3. Waits briefly for the clipboard to settle — some applications poll the clipboard asynchronously.
/// 4. Synthesizes a Ctrl+V keystroke sequence using the Win32 <c>SendInput</c> API, which injects
///    hardware-level keyboard events into the input queue of the foreground window.
/// 5. Waits again for the target application to process the paste.
/// 6. Restores the original clipboard contents in a <c>finally</c> block to guarantee cleanup
///    even if an exception occurs.
///
/// If clipboard access fails (e.g., Remote Desktop, clipboard managers), falls back to
/// character-by-character input via <c>SendInput</c> with <c>KEYEVENTF_UNICODE</c>.
///
/// All clipboard operations are dispatched to the WPF UI thread because the clipboard is
/// per-thread (STA) and must be accessed from the thread that owns it.
/// </summary>
public class TextInsertionService : ITextInsertionService
{
    private readonly ILogger<TextInsertionService> _logger;
    private readonly IOptionsMonitor<VoxwrightOptions> _optionsMonitor;
    private readonly IWindowFocusService _windowFocusService;

    /// <summary>
    /// Initializes a new instance of the <see cref="TextInsertionService"/> class.
    /// </summary>
    public TextInsertionService(
        ILogger<TextInsertionService> logger,
        IOptionsMonitor<VoxwrightOptions> optionsMonitor,
        IWindowFocusService windowFocusService)
    {
        _logger = logger;
        _optionsMonitor = optionsMonitor;
        _windowFocusService = windowFocusService;
    }

    /// <inheritdoc />
    public async Task<bool> InsertTextAsync(string text, IntPtr targetWindow = default)
    {
        _logger.LogInformation("Inserting text via clipboard ({Length} chars)", text.Length);
        var timing = _optionsMonitor.CurrentValue.Timing;

        IDataObject? previousClipboard = null;

        try
        {
            try
            {
                // Save and restore clipboard on the STA thread
                Application.Current.Dispatcher.Invoke(() =>
                {
                    try { previousClipboard = Clipboard.GetDataObject(); } catch (Exception ex) { _logger.LogDebug(ex, "Clipboard read failed before text insertion"); }
                    Clipboard.SetText(text);
                });
            }
            catch (COMException ex)
            {
                _logger.LogWarning(ex, "Clipboard unavailable, falling back to SendInput character-by-character");

                // Verify focus before character-by-character fallback too
                if (targetWindow != IntPtr.Zero && !await _windowFocusService.EnsureFocusAsync(targetWindow))
                {
                    _logger.LogWarning("Focus lost before SendInput fallback — aborting insertion");
                    return false;
                }

                await InsertViaSendInputAsync(text);
                return true;
            }

            // Brief delay for clipboard to settle
            await Task.Delay(timing.ClipboardSettleMs);

            // Verify focus right before sending keystrokes to close the race window
            if (targetWindow != IntPtr.Zero && !await _windowFocusService.EnsureFocusAsync(targetWindow))
            {
                _logger.LogWarning(
                    "Focus lost before paste — text left on clipboard for manual Ctrl+V");
                return false;
            }

            // Simulate Ctrl+V
            var inputs = new NativeMethods.INPUT[4];
            int size = Marshal.SizeOf<NativeMethods.INPUT>();

            // Ctrl down
            inputs[0].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].Union.Keyboard.VirtualKey = NativeMethods.VK_CONTROL;

            // V down
            inputs[1].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].Union.Keyboard.VirtualKey = NativeMethods.VK_V;

            // V up
            inputs[2].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[2].Union.Keyboard.VirtualKey = NativeMethods.VK_V;
            inputs[2].Union.Keyboard.Flags = NativeMethods.KEYEVENTF_KEYUP;

            // Ctrl up
            inputs[3].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[3].Union.Keyboard.VirtualKey = NativeMethods.VK_CONTROL;
            inputs[3].Union.Keyboard.Flags = NativeMethods.KEYEVENTF_KEYUP;

            var sent = NativeMethods.SendInput((uint)inputs.Length, inputs, size);
            if (sent != inputs.Length)
            {
                _logger.LogWarning("SendInput returned {Sent}/{Expected} — keystrokes may not have been delivered",
                    sent, inputs.Length);
            }

            // Wait for the paste to complete before restoring (shorter = less clipboard exposure)
            await Task.Delay(timing.PasteCompletionMs);
            return true;
        }
        finally
        {
            // Always restore previous clipboard content, even if SendInput or delays fail
            if (previousClipboard is not null)
            {
                try
                {
                    Application.Current.Dispatcher.Invoke(() => Clipboard.SetDataObject(previousClipboard, copy: true));
                }
                catch (Exception ex)
                {
                    _logger.LogDebug(ex, "Could not restore previous clipboard content");
                }
            }
        }
    }

    /// <summary>
    /// Fallback text insertion via SendInput with KEYEVENTF_UNICODE.
    /// Sends each character individually — slower than clipboard paste but works
    /// when clipboard access is blocked (Remote Desktop, clipboard managers).
    /// </summary>
    private async Task InsertViaSendInputAsync(string text)
    {
        _logger.LogInformation("Inserting text via SendInput Unicode fallback ({Length} chars)", text.Length);
        int size = Marshal.SizeOf<NativeMethods.INPUT>();

        foreach (char c in text)
        {
            var inputs = new NativeMethods.INPUT[2];

            // Key down
            inputs[0].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[0].Union.Keyboard.ScanCode = (ushort)c;
            inputs[0].Union.Keyboard.Flags = NativeMethods.KEYEVENTF_UNICODE;

            // Key up
            inputs[1].Type = NativeMethods.INPUT_KEYBOARD;
            inputs[1].Union.Keyboard.ScanCode = (ushort)c;
            inputs[1].Union.Keyboard.Flags = NativeMethods.KEYEVENTF_UNICODE | NativeMethods.KEYEVENTF_KEYUP;

            NativeMethods.SendInput(2, inputs, size);
        }

        // Brief delay for the input queue to flush
        await Task.Delay(10);
    }
}
