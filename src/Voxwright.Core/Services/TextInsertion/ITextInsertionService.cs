namespace Voxwright.Core.Services.TextInsertion;

/// <summary>
/// Inserts transcribed text at the cursor position in the previously active window
/// via clipboard and simulated Ctrl+V keystroke.
/// </summary>
public interface ITextInsertionService
{
    /// <summary>
    /// Inserts the specified text at the current cursor position in the foreground window.
    /// When <paramref name="targetWindow"/> is provided, verifies focus is still on the target
    /// right before sending keystrokes. Returns false if focus could not be confirmed — the text
    /// is left on the clipboard so the user can paste manually.
    /// </summary>
    /// <param name="text">The text to insert.</param>
    /// <param name="targetWindow">Optional HWND of the expected target window for focus verification.</param>
    /// <returns>True if text was inserted; false if insertion was aborted due to focus loss.</returns>
    Task<bool> InsertTextAsync(string text, IntPtr targetWindow = default);
}
