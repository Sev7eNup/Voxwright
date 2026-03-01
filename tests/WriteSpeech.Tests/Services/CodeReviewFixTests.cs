using System.IO;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using WriteSpeech.Core.Configuration;
using WriteSpeech.Core.Services.Audio;
using WriteSpeech.Core.Services.Modes;
using WriteSpeech.Core.Services.TextCorrection;
using WriteSpeech.Core.Services.IDE;
using WriteSpeech.Tests.TestHelpers;

namespace WriteSpeech.Tests.Services;

/// <summary>
/// Tests for fixes identified in the code review.
/// </summary>
public class CodeReviewFixTests
{
    // --- H1: ModeService.Dispose() flushes pending saves ---

    [Fact]
    public async Task ModeService_Dispose_FlushesPendingSaves()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"writespeech-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(tempDir);
        var filePath = Path.Combine(tempDir, "modes.json");

        try
        {
            var optionsMonitor = OptionsHelper.CreateMonitor(_ => { });
            var service = new ModeService(NullLogger<ModeService>.Instance, optionsMonitor);

            // Set file path via reflection (same pattern as ModeServiceTests)
            var field = typeof(ModeService).GetField("_filePath",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance)!;
            field.SetValue(service, filePath);

            await service.LoadAsync();

            // Add a mode (triggers debounced save)
            service.AddMode("FlushTest", "test prompt", ["testapp"]);

            // Dispose should flush immediately (no need to wait for debounce)
            service.Dispose();

            // The file should exist with the saved mode
            File.Exists(filePath).Should().BeTrue("Dispose should flush pending saves to disk");

            var content = await File.ReadAllTextAsync(filePath);
            content.Should().Contain("FlushTest");
        }
        finally
        {
            try { Directory.Delete(tempDir, recursive: true); } catch { }
        }
    }

    // --- H2: AudioRecordingService Division-by-Zero Guard ---

    [Fact]
    public void ConvertBytesToFloats_ZeroBytes_ReturnsEmptyArray()
    {
        var result = AudioRecordingService.ConvertBytesToFloats([], 0);

        result.Should().BeEmpty();
    }

    // --- H3: LocalTextCorrectionService <transcription> tag wrapping ---
    // (Tested indirectly via CloudTextCorrectionServiceBaseTests patterns)

    // --- M1: VoiceActivityService CreateDetector dedup ---
    // (Behavioral tests unchanged — the refactoring preserves existing behavior)

    // --- M2: ModeService.SaveAsync dead code removal ---
    // (Behavioral tests unchanged — the fix removes unreachable code)
}
