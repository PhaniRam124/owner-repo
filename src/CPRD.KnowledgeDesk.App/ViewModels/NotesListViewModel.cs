using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class NotesListViewModel : ObservableObject
{
    private readonly INoteService _notes;
    private Guid? _folderId;

    public NotesListViewModel(INoteService notes) => _notes = notes;

    public ObservableCollection<Note> Items { get; } = new();

    public Guid? FolderId
    {
        get => _folderId;
        private set => SetProperty(ref _folderId, value);
    }

    public async Task LoadAsync(Guid folderId)
    {
        FolderId = folderId;
        Items.Clear();
        foreach (var note in await _notes.ListByFolderAsync(folderId, CancellationToken.None))
            Items.Add(note);
    }
}
