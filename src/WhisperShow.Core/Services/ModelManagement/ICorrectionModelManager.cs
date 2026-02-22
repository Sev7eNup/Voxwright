using WhisperShow.Core.Models;

namespace WhisperShow.Core.Services.ModelManagement;

public interface ICorrectionModelManager
{
    IReadOnlyList<CorrectionModelInfo> GetAllModels();
    Task DownloadModelAsync(string fileName, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
    void DeleteModel(CorrectionModelInfo model);
    string ModelDirectory { get; }
}
