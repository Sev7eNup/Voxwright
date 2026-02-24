using WriteSpeech.Core.Services;

namespace WriteSpeech.Tests.TestHelpers;

public class SynchronousDispatcherService : IDispatcherService
{
    public void Invoke(Action action) => action();
    public Task InvokeAsync(Func<Task> asyncAction) => asyncAction();
}
