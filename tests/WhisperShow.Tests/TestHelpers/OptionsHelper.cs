using Microsoft.Extensions.Options;
using WhisperShow.Core.Configuration;

namespace WhisperShow.Tests.TestHelpers;

public static class OptionsHelper
{
    public static IOptions<WhisperShowOptions> Create(Action<WhisperShowOptions>? configure = null)
    {
        var options = new WhisperShowOptions();
        configure?.Invoke(options);
        return Options.Create(options);
    }

    public static IOptionsMonitor<WhisperShowOptions> CreateMonitor(Action<WhisperShowOptions>? configure = null)
    {
        var options = new WhisperShowOptions();
        configure?.Invoke(options);
        return new TestOptionsMonitor<WhisperShowOptions>(options);
    }
}

/// <summary>
/// Simple IOptionsMonitor implementation for tests that returns a fixed value.
/// </summary>
internal class TestOptionsMonitor<T> : IOptionsMonitor<T>
{
    public TestOptionsMonitor(T currentValue) => CurrentValue = currentValue;

    public T CurrentValue { get; }
    public T Get(string? name) => CurrentValue;
    public IDisposable? OnChange(Action<T, string?> listener) => null;
}
