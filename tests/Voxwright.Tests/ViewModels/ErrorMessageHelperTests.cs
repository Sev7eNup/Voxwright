using System.ClientModel;
using System.Net.Http;
using FluentAssertions;
using Voxwright.App.ViewModels;

namespace Voxwright.Tests.ViewModels;

public class ErrorMessageHelperTests
{
    [Fact]
    public void HttpRequestException_ReturnsNetworkError()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(new HttpRequestException("Connection refused"));
        result.Should().Be("Network error — check your internet connection.");
    }

    [Fact]
    public void TaskCanceledException_ReturnsTimedOut()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(new TaskCanceledException());
        result.Should().Be("Operation timed out.");
    }

    [Theory]
    [InlineData("API key is missing")]
    [InlineData("No API KEY configured")]
    [InlineData("Invalid api key provided")]
    public void InvalidOperationException_ApiKey_ReturnsApiKeyMessage(string message)
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(new InvalidOperationException(message));
        result.Should().Be("API key is not configured.");
    }

    [Fact]
    public void InvalidOperationException_HashMismatch_ReturnsCorruptedMessage()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(
            new InvalidOperationException("Downloaded file hash mismatch"));
        result.Should().Be("Downloaded file is corrupted. Please try again.");
    }

    [Fact]
    public void InvalidOperationException_MaximumSize_ReturnsTooLargeMessage()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(
            new InvalidOperationException("Input exceeds maximum size limit"));
        result.Should().Be("File is too large to process.");
    }

    [Fact]
    public void InvalidOperationException_VadModel_ReturnsVadMessage()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(
            new InvalidOperationException("VAD model is not available"));
        result.Should().Be("VAD model not downloaded. Enable hands-free mode in Settings to download it.");
    }

    [Fact]
    public void UnknownException_ReturnsFallbackMessage()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(new ArgumentException("something"));
        result.Should().Be("An unexpected error occurred. Check the log for details.");
    }

    [Fact]
    public void InvalidOperationException_UnmatchedMessage_ReturnsFallback()
    {
        var result = ErrorMessageHelper.SanitizeErrorMessage(
            new InvalidOperationException("Some other error"));
        result.Should().Be("An unexpected error occurred. Check the log for details.");
    }

    [Fact]
    public void ClientResultException_401_ReturnsInvalidApiKey()
    {
        var ex = new ClientResultException("HTTP 401 (invalid_api_key)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Invalid API key. Check your key in Settings.");
    }

    [Fact]
    public void ClientResultException_403_ReturnsAccessDenied()
    {
        var ex = new ClientResultException("HTTP 403 (forbidden)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Access denied. Check your API key permissions.");
    }

    [Fact]
    public void ClientResultException_429_ReturnsRateLimit()
    {
        var ex = new ClientResultException("HTTP 429 (rate_limit_exceeded)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Rate limit exceeded. Please wait and try again.");
    }

    [Fact]
    public void ClientResultException_400_WithModel_ReturnsInvalidModel()
    {
        var ex = new ClientResultException("HTTP 400 (invalid_model: model not found)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Invalid model configured. Check Settings.");
    }

    [Fact]
    public void ClientResultException_400_Generic_ReturnsBadRequest()
    {
        var ex = new ClientResultException("HTTP 400 (bad_request)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Bad request — check your provider settings.");
    }

    [Fact]
    public void ClientResultException_500_ReturnsServerError()
    {
        var ex = new ClientResultException("HTTP 500 (internal_server_error)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Provider server error. Please try again later.");
    }

    [Fact]
    public void ClientResultException_502_ReturnsServerError()
    {
        var ex = new ClientResultException("HTTP 502 (bad_gateway)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Provider server error. Please try again later.");
    }

    [Fact]
    public void ClientResultException_503_ReturnsServerError()
    {
        var ex = new ClientResultException("HTTP 503 (service_unavailable)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("Provider server error. Please try again later.");
    }

    [Fact]
    public void ClientResultException_OtherStatus_ReturnsGenericApiError()
    {
        var ex = new ClientResultException("HTTP 418 (i_am_a_teapot)");
        var result = ErrorMessageHelper.SanitizeErrorMessage(ex);
        result.Should().Be("API error. Check the log for details.");
    }
}
