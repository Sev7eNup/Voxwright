using Whisper.net.Ggml;
using WhisperShow.Core.Models;

namespace WhisperShow.App.ViewModels;

public class ModelItemViewModel : ModelItemViewModelBase
{
    public GgmlType GgmlType { get; }

    public ModelItemViewModel(WhisperModel model, GgmlType ggmlType)
        : base(model.Name, model.FileName, model.SizeDisplay, model.IsDownloaded)
    {
        GgmlType = ggmlType;
    }
}
