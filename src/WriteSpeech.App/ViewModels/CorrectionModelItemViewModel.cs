using WriteSpeech.Core.Models;

namespace WriteSpeech.App.ViewModels;

public class CorrectionModelItemViewModel : ModelItemViewModelBase
{
    public CorrectionModelItemViewModel(CorrectionModelInfo model)
        : base(model.Name, model.FileName, model.SizeDisplay, model.IsDownloaded)
    {
    }
}
