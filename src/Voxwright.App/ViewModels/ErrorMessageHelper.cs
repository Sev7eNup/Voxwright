using System.ClientModel;
using System.Net.Http;

namespace Voxwright.App.ViewModels;

/// <summary>
/// Converts raw exception messages into user-friendly error strings suitable for display
/// in the overlay UI. Maps common exception types (network errors, timeouts, missing API keys,
/// corrupted downloads, API errors, etc.) to concise, actionable messages without exposing
/// technical details.
/// </summary>
internal static class ErrorMessageHelper
{
    /// <summary>
    /// Returns a user-friendly error message for the given exception.
    /// Recognized exceptions (network, timeout, missing API key, corrupted download, oversized file,
    /// missing VAD model, API errors) produce specific messages; all others return a generic fallback.
    /// </summary>
    internal static string SanitizeErrorMessage(Exception ex) => ex switch
    {
        HttpRequestException => "Network error — check your internet connection.",
        TaskCanceledException => "Operation timed out.",
        InvalidOperationException e when e.Message.Contains("API key", StringComparison.OrdinalIgnoreCase)
            => "API key is not configured.",
        InvalidOperationException e when e.Message.Contains("hash mismatch", StringComparison.OrdinalIgnoreCase)
            => "Downloaded file is corrupted. Please try again.",
        InvalidOperationException e when e.Message.Contains("maximum size", StringComparison.OrdinalIgnoreCase)
            => "File is too large to process.",
        InvalidOperationException e when e.Message.Contains("VAD model", StringComparison.OrdinalIgnoreCase)
            => "VAD model not downloaded. Enable hands-free mode in Settings to download it.",
        ClientResultException e when e.Message.Contains("401")
            => "Invalid API key. Check your key in Settings.",
        ClientResultException e when e.Message.Contains("403")
            => "Access denied. Check your API key permissions.",
        ClientResultException e when e.Message.Contains("429")
            => "Rate limit exceeded. Please wait and try again.",
        ClientResultException e when e.Message.Contains("400") && e.Message.Contains("model", StringComparison.OrdinalIgnoreCase)
            => "Invalid model configured. Check Settings.",
        ClientResultException e when e.Message.Contains("400")
            => "Bad request — check your provider settings.",
        ClientResultException e when e.Message.Contains("500") || e.Message.Contains("502") || e.Message.Contains("503")
            => "Provider server error. Please try again later.",
        ClientResultException
            => "API error. Check the log for details.",
        _ => "An unexpected error occurred. Check the log for details."
    };
}
