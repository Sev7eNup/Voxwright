using System.IO;
using System.Net.Http;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using Voxwright.Core.Services.ModelManagement;

namespace Voxwright.Tests.Services;

public class ModelDownloadHelperTests : IDisposable
{
    private readonly string _tempDir;
    private readonly ModelDownloadHelper _helper;

    public ModelDownloadHelperTests()
    {
        _tempDir = Path.Combine(Path.GetTempPath(), $"writespeech-download-test-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tempDir);

        var httpFactory = Substitute.For<IHttpClientFactory>();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(new HttpClient());
        _helper = new ModelDownloadHelper(httpFactory, NullLogger<ModelDownloadHelper>.Instance);
    }

    public void Dispose()
    {
        try { Directory.Delete(_tempDir, recursive: true); } catch { }
    }

    [Fact]
    public async Task DownloadToFileAsync_WritesCompleteStream()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "test-download.bin");

        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length);

        var written = await File.ReadAllBytesAsync(targetPath);
        written.Should().Equal(data);
    }

    [Fact]
    public async Task DownloadToFileAsync_ReportsProgressToCompletion()
    {
        var data = new byte[100_000]; // Large enough for multiple buffer reads
        Array.Fill(data, (byte)42);
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "progress-test.bin");

        var progressValues = new List<float>();
        IProgress<float> progress = new SyncProgress<float>(v => progressValues.Add(v));

        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, progress);

        progressValues.Should().NotBeEmpty();
        progressValues.Last().Should().BeApproximately(1.0f, 0.01f);
    }

    [Fact]
    public async Task DownloadToFileAsync_CancellationStopsDownload()
    {
        var data = new byte[1_000_000];
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "cancel-test.bin");
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        var act = () => _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, cancellationToken: cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task DownloadToFileAsync_EmptyStream_CreatesEmptyFile()
    {
        using var sourceStream = new MemoryStream([]);
        var targetPath = Path.Combine(_tempDir, "empty-test.bin");

        await _helper.DownloadToFileAsync(sourceStream, targetPath, 0);

        File.Exists(targetPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(targetPath)).Should().BeEmpty();
    }

    [Fact]
    public async Task DownloadToFileAsync_WithMatchingHash_Succeeds()
    {
        var data = new byte[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 };
        var expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data));
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "hash-test.bin");

        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, expectedSha256: expectedHash);

        File.Exists(targetPath).Should().BeTrue();
        (await File.ReadAllBytesAsync(targetPath)).Should().Equal(data);
    }

    [Fact]
    public async Task DownloadToFileAsync_WithMismatchedHash_ThrowsAndDeletesTempFile()
    {
        var data = new byte[] { 1, 2, 3, 4, 5 };
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "bad-hash-test.bin");

        var act = () => _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length,
            expectedSha256: "0000000000000000000000000000000000000000000000000000000000000000");

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*hash mismatch*");
        File.Exists(targetPath).Should().BeFalse();
        File.Exists(targetPath + ".downloading").Should().BeFalse();
    }

    [Fact]
    public async Task DownloadToFileAsync_WithNullHash_StoresTofuSidecar()
    {
        var data = new byte[] { 1, 2, 3 };
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "no-hash-test.bin");

        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, expectedSha256: null);

        File.Exists(targetPath).Should().BeTrue();
        var sidecar = targetPath + ".sha256";
        File.Exists(sidecar).Should().BeTrue();
        var storedHash = File.ReadAllText(sidecar);
        var expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data));
        storedHash.Should().Be(expectedHash);
    }

    [Fact]
    public async Task DownloadToFileAsync_WithTofuSidecar_VerifiesHash()
    {
        var data = new byte[] { 10, 20, 30 };
        var targetPath = Path.Combine(_tempDir, "tofu-verify.bin");
        var hash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data));
        File.WriteAllText(targetPath + ".sha256", hash);

        using var sourceStream = new MemoryStream(data);
        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, expectedSha256: null);

        File.Exists(targetPath).Should().BeTrue();
    }

    [Fact]
    public async Task DownloadToFileAsync_WithTofuMismatch_Throws()
    {
        var data = new byte[] { 10, 20, 30 };
        var targetPath = Path.Combine(_tempDir, "tofu-mismatch.bin");
        File.WriteAllText(targetPath + ".sha256", "0000000000000000000000000000000000000000000000000000000000000000");

        using var sourceStream = new MemoryStream(data);
        var act = () => _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, expectedSha256: null);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("*hash mismatch*");
    }

    [Fact]
    public async Task DownloadToFileAsync_WithHardcodedHash_DoesNotStoreTofuSidecar()
    {
        var data = new byte[] { 5, 10, 15 };
        var expectedHash = Convert.ToHexStringLower(System.Security.Cryptography.SHA256.HashData(data));
        using var sourceStream = new MemoryStream(data);
        var targetPath = Path.Combine(_tempDir, "hardcoded-hash.bin");

        await _helper.DownloadToFileAsync(sourceStream, targetPath, data.Length, expectedSha256: expectedHash);

        File.Exists(targetPath + ".sha256").Should().BeFalse();
    }

    [Fact]
    public void CreateClient_SetsTimeout()
    {
        var httpFactory = Substitute.For<IHttpClientFactory>();
        var client = new HttpClient();
        httpFactory.CreateClient(Arg.Any<string>()).Returns(client);
        var helper = new ModelDownloadHelper(httpFactory, NullLogger<ModelDownloadHelper>.Instance);

        var result = helper.CreateClient(TimeSpan.FromMinutes(10));

        result.Timeout.Should().Be(TimeSpan.FromMinutes(10));
    }

    private class SyncProgress<T>(Action<T> handler) : IProgress<T>
    {
        public void Report(T value) => handler(value);
    }
}
