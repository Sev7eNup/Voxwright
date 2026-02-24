namespace WriteSpeech.Core.Services.TextInsertion;

public interface ITextInsertionService
{
    Task InsertTextAsync(string text);
}
