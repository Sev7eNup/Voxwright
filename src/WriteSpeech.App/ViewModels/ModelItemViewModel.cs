using Whisper.net.Ggml;
using WriteSpeech.Core.Models;

namespace WriteSpeech.App.ViewModels;

public class ModelItemViewModel : ModelItemViewModelBase
{
    public GgmlType GgmlType { get; }

    public ModelItemViewModel(WhisperModel model, GgmlType ggmlType)
        : base(model.Name, model.FileName, model.SizeDisplay, model.IsDownloaded)
    {
        GgmlType = ggmlType;
    }
}
