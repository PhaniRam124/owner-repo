using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class QuickCaptureViewModel : ObservableObject
{
    private static readonly string[] ApprovedNoteTypes =
    {
        "Standard Note",
        "Portal / Website Reference",
        "Vendor Note",
        "Payment Note",
        "Meeting Note",
        "Checklist"
    };

    private readonly INoteService _notes;
    private readonly IFolderService _folders;
    private string _title = string.Empty;
    private string _plainText = string.Empty;
    private string _tagsText = string.Empty;
    private string _selectedNoteType = ApprovedNoteTypes[0];
    private Folder? _selectedFolder;
    private bool _isPinned;
    private bool _isFavorite;
    private string _status = "Ready";

    public QuickCaptureViewModel(INoteService notes, IFolderService folders)
    {
        _notes = notes;
        _folders = folders;
        SaveCommand = new AsyncRelayCommand(SaveAsync, CanSave);
    }

    public event EventHandler<Guid>? Saved;

    public ObservableCollection<Folder> Folders { get; } = new();
    public IReadOnlyList<string> NoteTypes => ApprovedNoteTypes;
    public IAsyncRelayCommand SaveCommand { get; }

    public string Title
    {
        get => _title;
        set
        {
            if (SetProperty(ref _title, value))
                SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public string PlainText { get => _plainText; set => SetProperty(ref _plainText, value); }
    public string TagsText { get => _tagsText; set => SetProperty(ref _tagsText, value); }
    public string SelectedNoteType { get => _selectedNoteType; set => SetProperty(ref _selectedNoteType, value); }

    public Folder? SelectedFolder
    {
        get => _selectedFolder;
        set
        {
            if (SetProperty(ref _selectedFolder, value))
                SaveCommand.NotifyCanExecuteChanged();
        }
    }

    public bool IsPinned { get => _isPinned; set => SetProperty(ref _isPinned, value); }
    public bool IsFavorite { get => _isFavorite; set => SetProperty(ref _isFavorite, value); }
    public string Status { get => _status; private set => SetProperty(ref _status, value); }

    public async Task InitializeAsync()
    {
        Folders.Clear();
        foreach (var folder in await _folders.GetTreeAsync(CancellationToken.None))
            Folders.Add(folder);

        SelectedFolder = Folders.FirstOrDefault(folder => folder.Name.Equals("General", StringComparison.OrdinalIgnoreCase))
            ?? Folders.FirstOrDefault();
    }

    private bool CanSave() => SelectedFolder is not null && (!string.IsNullOrWhiteSpace(Title) || !string.IsNullOrWhiteSpace(PlainText));

    private async Task SaveAsync()
    {
        if (SelectedFolder is null) return;

        var tags = TagsText
            .Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        Status = "Saving…";
        var note = await _notes.CreateAsync(
            new NewNoteRequest(
                Title,
                string.Empty,
                PlainText,
                SelectedFolder.Id,
                SelectedNoteType,
                "{}",
                tags),
            CancellationToken.None);

        if (IsPinned || IsFavorite)
        {
            await _notes.UpdateAsync(
                new UpdateNoteRequest(
                    note.Id,
                    note.Title,
                    note.ContentPackage,
                    note.PlainText,
                    note.FolderId,
                    note.NoteType,
                    IsFavorite,
                    IsPinned,
                    note.StructuredJson,
                    tags),
                CancellationToken.None);
        }

        Status = "Saved";
        Saved?.Invoke(this, note.Id);
    }
}
