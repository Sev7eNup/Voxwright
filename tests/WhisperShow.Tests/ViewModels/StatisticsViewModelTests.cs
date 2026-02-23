using System.Reflection;
using FluentAssertions;
using NSubstitute;
using WhisperShow.App.ViewModels.Settings;
using WhisperShow.Core.Models;
using WhisperShow.Core.Services.Statistics;

namespace WhisperShow.Tests.ViewModels;

public class StatisticsViewModelTests
{
    private readonly IUsageStatsService _statsService;
    private readonly StatisticsViewModel _vm;

    public StatisticsViewModelTests()
    {
        _statsService = Substitute.For<IUsageStatsService>();
        _vm = new StatisticsViewModel(_statsService);
    }

    [Fact]
    public void Refresh_PopulatesProperties()
    {
        _statsService.GetStats().Returns(new UsageStats
        {
            TotalTranscriptions = 42,
            TotalRecordingSeconds = 3661, // 1h 1m 1s
            ErrorCount = 3,
            TranscriptionsByProvider = new Dictionary<string, int>
            {
                ["OpenAI"] = 30,
                ["Local"] = 12
            }
        });

        _vm.RefreshCommand.Execute(null);

        var stats = _statsService.GetStats();

        _vm.TotalTranscriptions.Should().Be(42);
        _vm.ErrorCount.Should().Be(3);
        _vm.TotalRecordingTimeDisplay.Should().Be("1h 1m");
        // AverageRecordingSeconds = 3661 / 42 ≈ 87.2 — format is culture-dependent
        _vm.AverageDurationDisplay.Should().Be($"{stats.AverageRecordingSeconds:F1}s");
        // EstimatedApiCost = (3661 / 60.0) * 0.006 ≈ 0.3661 — format is culture-dependent
        _vm.EstimatedCostDisplay.Should().Be($"${stats.EstimatedApiCost:F4}");
    }

    [Fact]
    public void Refresh_FormatsProviderBreakdown_WithData()
    {
        _statsService.GetStats().Returns(new UsageStats
        {
            TotalTranscriptions = 5,
            TotalRecordingSeconds = 60,
            TranscriptionsByProvider = new Dictionary<string, int>
            {
                ["OpenAI"] = 3,
                ["Local"] = 2
            }
        });

        _vm.RefreshCommand.Execute(null);

        _vm.ProviderBreakdownDisplay.Should().Contain("OpenAI: 3");
        _vm.ProviderBreakdownDisplay.Should().Contain("Local: 2");
    }

    [Fact]
    public void Refresh_ShowsNoDataYet_WhenEmpty()
    {
        _statsService.GetStats().Returns(new UsageStats
        {
            TotalTranscriptions = 0,
            TotalRecordingSeconds = 0,
            TranscriptionsByProvider = new Dictionary<string, int>()
        });

        _vm.RefreshCommand.Execute(null);

        _vm.ProviderBreakdownDisplay.Should().Be("No data yet");
    }

    [Fact]
    public void Reset_CallsServiceReset_AndRefreshes()
    {
        _statsService.GetStats().Returns(new UsageStats
        {
            TotalTranscriptions = 0,
            TotalRecordingSeconds = 0,
            TranscriptionsByProvider = new Dictionary<string, int>()
        });

        _vm.ResetCommand.Execute(null);

        _statsService.Received(1).Reset();
        _statsService.Received(1).GetStats(); // Refresh calls GetStats
        _vm.TotalTranscriptions.Should().Be(0);
    }

    [Fact]
    public void FormatDuration_ReturnsMinutesSeconds_WhenUnderOneHour()
    {
        var method = typeof(StatisticsViewModel)
            .GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string)method.Invoke(null, [150.0])!; // 2m 30s

        result.Should().Be("2m 30s");
    }

    [Fact]
    public void FormatDuration_ReturnsHoursMinutes_WhenOverOneHour()
    {
        var method = typeof(StatisticsViewModel)
            .GetMethod("FormatDuration", BindingFlags.NonPublic | BindingFlags.Static)!;

        var result = (string)method.Invoke(null, [7380.0])!; // 2h 3m

        result.Should().Be("2h 3m");
    }
}
