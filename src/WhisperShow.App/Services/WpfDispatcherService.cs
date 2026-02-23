using System.Windows;
using WhisperShow.Core.Services;

namespace WhisperShow.App.Services;

public class WpfDispatcherService : IDispatcherService
{
    public void Invoke(Action action)
        => Application.Current?.Dispatcher.Invoke(action);
}
