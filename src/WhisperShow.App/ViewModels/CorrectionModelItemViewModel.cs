using CommunityToolkit.Mvvm.ComponentModel;
using WhisperShow.Core.Models;

namespace WhisperShow.App.ViewModels;

public partial class CorrectionModelItemViewModel : ObservableObject
{
    public string Name { get; }
    public string FileName { get; }
    public string SizeDisplay { get; }

    [ObservableProperty] private bool _isDownloaded;
    [ObservableProperty] private bool _isDownloading;
    [ObservableProperty] private bool _isActive;
    [ObservableProperty] private float _downloadProgress;
    [ObservableProperty] private string _statusText = "";

    public CorrectionModelItemViewModel(CorrectionModelInfo model)
    {
        Name = model.Name;
        FileName = model.FileName;
        SizeDisplay = model.SizeDisplay;
        IsDownloaded = model.IsDownloaded;
        StatusText = model.IsDownloaded ? "Downloaded" : "Not downloaded";
    }
}
