using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using WhisperShow.Core.Models;
using WhisperShow.Core.Services.History;
using WhisperShow.Core.Services.TextInsertion;

namespace WhisperShow.App.ViewModels;

public partial class HistoryViewModel : ObservableObject
{
    private readonly ITranscriptionHistoryService _historyService;
    private readonly ITextInsertionService _textInsertionService;

    public ObservableCollection<TranscriptionHistoryEntry> Entries { get; } = [];

    public HistoryViewModel(
        ITranscriptionHistoryService historyService,
        ITextInsertionService textInsertionService)
    {
        _historyService = historyService;
        _textInsertionService = textInsertionService;
    }

    public void Refresh()
    {
        Entries.Clear();
        foreach (var entry in _historyService.GetEntries())
            Entries.Add(entry);
    }

    [RelayCommand]
    private void CopyEntry(TranscriptionHistoryEntry entry)
    {
        Application.Current.Dispatcher.Invoke(() => Clipboard.SetText(entry.Text));
    }

    [RelayCommand]
    private async Task InsertEntry(TranscriptionHistoryEntry entry)
    {
        await _textInsertionService.InsertTextAsync(entry.Text);
    }

    [RelayCommand]
    private void RemoveEntry(TranscriptionHistoryEntry entry)
    {
        _historyService.RemoveEntry(entry);
        Entries.Remove(entry);
    }

    [RelayCommand]
    private void ClearAll()
    {
        _historyService.Clear();
        Entries.Clear();
    }
}
