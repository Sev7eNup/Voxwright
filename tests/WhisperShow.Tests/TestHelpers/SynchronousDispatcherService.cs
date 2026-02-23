using WhisperShow.Core.Services;

namespace WhisperShow.Tests.TestHelpers;

public class SynchronousDispatcherService : IDispatcherService
{
    public void Invoke(Action action) => action();
}
