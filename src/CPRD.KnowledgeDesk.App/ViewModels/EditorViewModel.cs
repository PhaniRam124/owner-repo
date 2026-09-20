using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class EditorViewModel : ObservableObject
{
    private readonly INoteService _notes;
    private Note? _note;
    private string _title = string.Empty;
    private string _contentPackage = string.Empty;
    private string _plainText = string.Empty;
    private string _structuredJson = "{}";
    private bool _isFavorite;
    private bool _isPinned;
    private string _saveStatus = "No note selected";

    public EditorViewModel(INoteService notes)
    {
        _notes = notes;
        SaveNowCommand = new AsyncRelayCommand(SaveAsync, () => _note is not null);
        ToggleFavoriteCommand = new RelayCommand(ToggleFavorite, () => _note is not null);
    }

    public IAsyncRelayCommand SaveNowCommand { get; }
    public IRelayCommand ToggleFavoriteCommand { get; }

    public Note? CurrentNote
    {
        get => _note;
        private set
        {
            if (SetProperty(ref _note, value))
            {
                SaveNowCommand.NotifyCanExecuteChanged();
                ToggleFavoriteCommand.NotifyCanExecuteChanged();
            }
        }
    }

    public string Title { get => _title; set => SetProperty(ref _title, value); }
    public string ContentPackage { get => _contentPackage; set => SetProperty(ref _contentPackage, value); }
    public string PlainText { get => _plainText; set => SetProperty(ref _plainText, value); }
    public string StructuredJson { get => _structuredJson; set => SetProperty(ref _structuredJson, value); }
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }
    public bool IsPinned { get => _isPinned; set => SetProperty(ref _isPinned, value); }
    public string SaveStatus { get => _saveStatus; private set => SetProperty(ref _saveStatus, value); }

    public void Load(Note note)
    {
        CurrentNote = note;
        Title = note.Title;
        ContentPackage = note.ContentPackage;
        PlainText = note.PlainText;
        StructuredJson = note.StructuredJson;
        IsFavorite = note.IsFavorite;
        IsPinned = note.IsPinned;
        SaveStatus = "Loaded";
    }

    public void Clear()
    {
        CurrentNote = null;
        Title = string.Empty;
        ContentPackage = string.Empty;
        PlainText = string.Empty;
        StructuredJson = "{}";
        IsFavorite = false;
        IsPinned = false;
        SaveStatus = "No note selected";
    }

    private void ToggleFavorite()
    {
        if (CurrentNote is null) return;
        IsFavorite = !IsFavorite;
        SaveStatus = IsFavorite ? "Favorite enabled" : "Favorite disabled";
    }

    private async Task SaveAsync()
    {
        if (CurrentNote is null) return;

        await _notes.UpdateAsync(new UpdateNoteRequest(
            CurrentNote.Id,
            Title,
            ContentPackage,
            PlainText,
            CurrentNote.FolderId,
            CurrentNote.NoteType,
            IsFavorite,
            IsPinned,
            StructuredJson,
            Array.Empty<string>()), CancellationToken.None);

        SaveStatus = $"Saved {DateTime.Now:t}";
    }
}
