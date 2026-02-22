using CommunityToolkit.Mvvm.ComponentModel;
using Whisper.net.Ggml;
using WhisperShow.Core.Models;

namespace WhisperShow.App.ViewModels;

public partial class ModelItemViewModel : ObservableObject
{
    public string Name { get; }
    public string FileName { get; }
    public string SizeDisplay { get; }
    public GgmlType GgmlType { get; }

    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private float _downloadProgress;
    [ObservableProperty] private string _statusText = "";

    public ModelItemViewModel(WhisperModel model, GgmlType ggmlType)
    {
        Name = model.Name;
        FileName = model.FileName;
        SizeDisplay = model.SizeDisplay;
        GgmlType = ggmlType;
        IsDownloaded = model.IsDownloaded;
        StatusText = model.IsDownloaded ? "Downloaded" : "Not downloaded";
    }
}
