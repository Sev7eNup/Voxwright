using WhisperShow.Core.Models;

namespace WhisperShow.App.ViewModels;

public class CorrectionModelItemViewModel : ModelItemViewModelBase
{
    public CorrectionModelItemViewModel(CorrectionModelInfo model)
        : base(model.Name, model.FileName, model.SizeDisplay, model.IsDownloaded)
    {
    }
}
