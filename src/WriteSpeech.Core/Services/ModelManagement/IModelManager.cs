using Whisper.net.Ggml;
using WriteSpeech.Core.Models;

namespace WriteSpeech.Core.Services.ModelManagement;

public interface IModelManager
{
    IReadOnlyList<WhisperModel> GetAvailableModels();
    IReadOnlyList<WhisperModel> GetAllModels();
    Task DownloadModelAsync(GgmlType type, IProgress<float>? progress = null, CancellationToken cancellationToken = default);
    void DeleteModel(WhisperModel model);
    string ModelDirectory { get; }
}
