using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class TrashViewModel : ObservableObject
{
    private readonly INoteService _notes;
    private string _status = "Trash";

    public TrashViewModel(INoteService notes)
    {
        _notes = notes;
        RefreshCommand = new AsyncRelayCommand(RefreshAsync);
        RestoreCommand = new AsyncRelayCommand<Guid>(RestoreAsync);
        DeletePermanentlyCommand = new AsyncRelayCommand<Guid>(DeletePermanentlyAsync);
        EmptyTrashCommand = new AsyncRelayCommand(EmptyTrashAsync, () => Items.Count > 0);
    }

    public ObservableCollection<Note> Items { get; } = new();
    public IAsyncRelayCommand RefreshCommand { get; }
    public AsyncRelayCommand<Guid> RestoreCommand { get; }
    public AsyncRelayCommand<Guid> DeletePermanentlyCommand { get; }
    public IAsyncRelayCommand EmptyTrashCommand { get; }

    public string Status
    {
        get => _status;
        private set => SetProperty(ref _status, value);
    }

    public async Task RefreshAsync()
    {
        Items.Clear();
        foreach (var note in await _notes.ListTrashAsync(CancellationToken.None))
            Items.Add(note);
        Status = Items.Count == 0 ? "Trash is empty" : $"{Items.Count} deleted note(s)";
        EmptyTrashCommand.NotifyCanExecuteChanged();
    }

    private async Task RestoreAsync(Guid id)
    {
        await _notes.RestoreFromTrashAsync(id, CancellationToken.None);
        await RefreshAsync();
        Status = "Note restored";
    }

    private async Task DeletePermanentlyAsync(Guid id)
    {
        await _notes.DeletePermanentlyAsync(id, CancellationToken.None);
        await RefreshAsync();
        Status = "Note permanently deleted";
    }

    private async Task EmptyTrashAsync()
    {
        var ids = Items.Select(note => note.Id).ToArray();
        foreach (var id in ids)
            await _notes.DeletePermanentlyAsync(id, CancellationToken.None);

        await RefreshAsync();
        Status = "Trash is empty";
    }
}
