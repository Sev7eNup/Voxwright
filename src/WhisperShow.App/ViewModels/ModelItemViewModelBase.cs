using CommunityToolkit.Mvvm.ComponentModel;

namespace WhisperShow.App.ViewModels;

public abstract partial class ModelItemViewModelBase : ObservableObject
{
    public string Name { get; }
    public string FileName { get; }
    public string SizeDisplay { get; }

    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private float _downloadProgress;
    [ObservableProperty] private string _statusText = "";

    protected ModelItemViewModelBase(string name, string fileName, string sizeDisplay, bool isDownloaded)
    {
        Name = name;
        FileName = fileName;
        SizeDisplay = sizeDisplay;
        IsDownloaded = isDownloaded;
        StatusText = isDownloaded ? "Downloaded" : "Not downloaded";
    }
}
