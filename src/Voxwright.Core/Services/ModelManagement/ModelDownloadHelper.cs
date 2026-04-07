using System.Security.Cryptography;
using Microsoft.Extensions.Logging;

namespace Voxwright.Core.Services.ModelManagement;

public class ModelDownloadHelper
{
    private const int BufferSize = 81920;

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<ModelDownloadHelper> _logger;

    public ModelDownloadHelper(IHttpClientFactory httpClientFactory, ILogger<ModelDownloadHelper> logger)
    {
        ArgumentNullException.ThrowIfNull(httpClientFactory);
        ArgumentNullException.ThrowIfNull(logger);
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    public async Task DownloadToFileAsync(
        Stream sourceStream,
        string targetPath,
        long expectedSize,
        IProgress<float>? progress = null,
        CancellationToken cancellationToken = default,
        string? expectedSha256 = null)
    {
        // TOFU: if no hardcoded hash is provided, check for a stored hash from a previous download
        if (expectedSha256 is null)
        {
            var tofuHashFile = targetPath + ".sha256";
            if (File.Exists(tofuHashFile))
            {
                expectedSha256 = File.ReadAllText(tofuHashFile).Trim();
                _logger.LogInformation("Using stored TOFU hash for {Path}: {Hash}", targetPath, expectedSha256);
            }
        }

        var tempPath = targetPath + ".downloading";
        try
        {
            // Always compute SHA-256: either for verification (when expected hash exists) or for TOFU storage
            using var sha256 = SHA256.Create();

            await using (var fileStream = File.Create(tempPath))
            {
                var buffer = new byte[BufferSize];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                    sha256.TransformBlock(buffer, 0, bytesRead, null, 0);
                    totalRead += bytesRead;
                    progress?.Report((float)totalRead / expectedSize);
                }

                sha256.TransformFinalBlock([], 0, 0);
                await fileStream.FlushAsync(cancellationToken);

                _logger.LogInformation("Download completed: {Size} bytes written to {Path}", totalRead, targetPath);
            }

            // Verify SHA-256 hash if expected hash is available (hardcoded or TOFU)
            if (expectedSha256 is not null)
            {
                var actualHash = Convert.ToHexStringLower(sha256.Hash!);
                if (!string.Equals(actualHash, expectedSha256, StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogError("Hash mismatch for {Path}: expected {Expected}, got {Actual}",
                        targetPath, expectedSha256, actualHash);
                    try { File.Delete(tempPath); } catch (Exception deleteEx) { _logger.LogDebug(deleteEx, "Failed to delete temp file after hash mismatch: {TempPath}", tempPath); }
                    throw new InvalidOperationException(
                        $"Downloaded file hash mismatch. Expected: {expectedSha256}, actual: {actualHash}");
                }

                _logger.LogInformation("SHA-256 verified for {Path}: {Hash}", targetPath, actualHash);
            }

            // Atomic rename: only move to final path after successful complete download + hash check
            File.Move(tempPath, targetPath, overwrite: true);

            // TOFU (Trust On First Use): when no expected hash was provided (first download),
            // store the computed hash so future re-downloads can be verified against it.
            if (expectedSha256 is null)
            {
                var computedHash = Convert.ToHexStringLower(sha256.Hash!);
                try
                {
                    File.WriteAllText(targetPath + ".sha256", computedHash);
                    _logger.LogInformation("Stored TOFU SHA-256 for {Path}: {Hash}", targetPath, computedHash);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to store TOFU hash for {Path}", targetPath);
                }
            }
        }
        catch
        {
            // Clean up partial temp file on failure or cancellation
            try { File.Delete(tempPath); } catch (Exception deleteEx) { _logger.LogDebug(deleteEx, "Failed to delete partial temp file: {TempPath}", tempPath); }
            throw;
        }
    }

    /// <summary>
    /// Verifies a file against its TOFU <c>.sha256</c> sidecar. Returns true if no sidecar exists
    /// (first use) or if the hash matches. Returns false if the hash mismatches.
    /// </summary>
    internal bool VerifyTofuHash(string filePath)
    {
        var hashFile = filePath + ".sha256";
        if (!File.Exists(hashFile))
            return true;

        try
        {
            var expectedHash = File.ReadAllText(hashFile).Trim();
            using var stream = File.OpenRead(filePath);
            var actualHash = Convert.ToHexStringLower(SHA256.HashData(stream));

            if (string.Equals(actualHash, expectedHash, StringComparison.OrdinalIgnoreCase))
                return true;

            _logger.LogError("TOFU hash mismatch for {Path}: expected {Expected}, got {Actual}",
                filePath, expectedHash, actualHash);
            return false;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to verify TOFU hash for {Path}", filePath);
            return true; // Don't block on verification errors
        }
    }

    public HttpClient CreateClient(TimeSpan? timeout = null)
    {
        var client = _httpClientFactory.CreateClient("ModelDownload");
        if (timeout.HasValue)
            client.Timeout = timeout.Value;
        return client;
    }
}
