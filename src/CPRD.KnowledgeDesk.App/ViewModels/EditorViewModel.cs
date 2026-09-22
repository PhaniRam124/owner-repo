using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CPRD.KnowledgeDesk.App.Services;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;

namespace CPRD.KnowledgeDesk.App.ViewModels;

public sealed class EditorViewModel : ObservableObject, IDisposable
{
    private readonly INoteService _notes;
    private readonly IRevisionService? _revisions;
    private readonly AutoSaveCoordinator _autoSave;
    private readonly IRecoveryService? _recovery;
    private readonly IAppLogService? _log;
    private readonly AutoSaveCoordinator? _recoveryCoordinator;
    private readonly bool _ownsAutoSave;
    private Note? _note;
    private string _title = string.Empty;
    private string _contentPackage = string.Empty;
    private string _plainText = string.Empty;
    private string _structuredJson = "{}";
    private string _tagsText = string.Empty;
    private string _savedTagsFingerprint = string.Empty;
    private bool _isFavorite;
    private bool _isPinned;
    private string _saveState = "Saved";
    private string _saveStatus = "No note selected";
    private bool _loading;
    private int _documentVersion;
    private DateTimeOffset _lastRevisionAtUtc = DateTimeOffset.MinValue;

    public EditorViewModel(INoteService notes)
        : this(
            notes,
            null,
            new AutoSaveCoordinator(new SystemDelayScheduler(), TimeSpan.FromMilliseconds(750)),
            null,
            null,
            true)
    {
    }

    public EditorViewModel(INoteService notes, IRevisionService revisions, AutoSaveCoordinator autoSave)
        : this(notes, revisions, autoSave, null, null, false)
    {
    }

    public EditorViewModel(
        INoteService notes,
        IRevisionService revisions,
        AutoSaveCoordinator autoSave,
        IRecoveryService recovery)
        : this(notes, revisions, autoSave, recovery, null, false)
    {
    }

    public EditorViewModel(
        INoteService notes,
        IRevisionService revisions,
        AutoSaveCoordinator autoSave,
        IRecoveryService recovery,
        IAppLogService log)
        : this(notes, revisions, autoSave, recovery, log, false)
    {
    }

    private EditorViewModel(
        INoteService notes,
        IRevisionService? revisions,
        AutoSaveCoordinator autoSave,
        IRecoveryService? recovery,
        IAppLogService? log,
        bool ownsAutoSave)
    {
        _notes = notes;
        _revisions = revisions;
        _autoSave = autoSave;
        _recovery = recovery;
        _log = log;
        _recoveryCoordinator = recovery is null
            ? null
            : new AutoSaveCoordinator(
                new SystemDelayScheduler(),
                TimeSpan.FromMilliseconds(200));
        _ownsAutoSave = ownsAutoSave;
        SaveNowCommand = new AsyncRelayCommand(() => FlushAsync(true, CancellationToken.None), () => _note is not null);
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

    public string Title
    {
        get => _title;
        set { if (SetProperty(ref _title, value)) MarkDirty(); }
    }

    public string ContentPackage
    {
        get => _contentPackage;
        set { if (SetProperty(ref _contentPackage, value)) MarkDirty(); }
    }

    public string PlainText
    {
        get => _plainText;
        set { if (SetProperty(ref _plainText, value)) MarkDirty(); }
    }

    public string StructuredJson
    {
        get => _structuredJson;
        set { if (SetProperty(ref _structuredJson, value)) MarkDirty(); }
    }

    public string TagsText
    {
        get => _tagsText;
        set { if (SetProperty(ref _tagsText, value)) MarkDirty(); }
    }

    public bool IsFavorite
    {
        get => _isFavorite;
        set { if (SetProperty(ref _isFavorite, value)) MarkDirty(); }
    }

    public bool IsPinned
    {
        get => _isPinned;
        set { if (SetProperty(ref _isPinned, value)) MarkDirty(); }
    }

    public string SaveState
    {
        get => _saveState;
        private set => SetProperty(ref _saveState, value);
    }

    public string SaveStatus
    {
        get => _saveStatus;
        private set => SetProperty(ref _saveStatus, value);
    }

    public int DocumentVersion
    {
        get => _documentVersion;
        private set => SetProperty(ref _documentVersion, value);
    }

    public void Load(Note note)
    {
        _loading = true;
        try
        {
            CurrentNote = note;
            Title = note.Title;
            ContentPackage = note.ContentPackage;
            PlainText = note.PlainText;
            StructuredJson = note.StructuredJson;
            TagsText = string.Empty;
            _savedTagsFingerprint = string.Empty;
            IsFavorite = note.IsFavorite;
            IsPinned = note.IsPinned;
            SaveState = "Saved";
            SaveStatus = "Saved";
            DocumentVersion++;
        }
        finally
        {
            _loading = false;
        }
    }

    public void LoadTags(IEnumerable<string> tags)
    {
        _loading = true;
        try
        {
            var normalized = NormalizeTags(tags);
            TagsText = string.Join(", ", normalized);
            _savedTagsFingerprint = string.Join("|", normalized.Select(value => value.ToUpperInvariant()));
        }
        finally
        {
            _loading = false;
        }
    }

    public void Clear()
    {
        _loading = true;
        try
        {
            CurrentNote = null;
            Title = string.Empty;
            ContentPackage = string.Empty;
            PlainText = string.Empty;
            StructuredJson = "{}";
            TagsText = string.Empty;
            _savedTagsFingerprint = string.Empty;
            IsFavorite = false;
            IsPinned = false;
            SaveState = "Saved";
            SaveStatus = "No note selected";
            DocumentVersion++;
        }
        finally
        {
            _loading = false;
        }
    }

    public Task FlushAsync(bool createRevision, CancellationToken cancellationToken) =>
        _autoSave.FlushAsync(ct => SaveCoreAsync(createRevision, ct), cancellationToken);

    private void ToggleFavorite()
    {
        if (CurrentNote is null) return;
        IsFavorite = !IsFavorite;
    }

    private void MarkDirty()
    {
        if (_loading || CurrentNote is null) return;

        SaveState = "Unsaved";
        SaveStatus = "Unsaved changes";

        if (_recovery is not null &&
            _recoveryCoordinator is not null &&
            CurrentNote is { } recoveryNote)
        {
            var draft = new RecoveryDraft(
                recoveryNote.Id,
                Title,
                ContentPackage,
                PlainText,
                StructuredJson,
                DateTimeOffset.UtcNow);

            _ = _recoveryCoordinator.ScheduleAsync(
                ct => _recovery.SaveDraftAsync(draft, ct));
        }

        _ = _autoSave.ScheduleAsync(ct => SaveCoreAsync(false, ct));
    }

    private async Task SaveCoreAsync(bool forceRevision, CancellationToken cancellationToken)
    {
        var current = CurrentNote;
        if (current is null || !HasChanges(current))
        {
            SaveState = "Saved";
            SaveStatus = current is null ? "No note selected" : "Saved";
            return;
        }

        SaveState = "Saving";
        SaveStatus = "Saving...";

        try
        {
            if (_recoveryCoordinator is not null)
                await _recoveryCoordinator.FlushAsync(cancellationToken);

            var now = DateTimeOffset.UtcNow;
            var revisionDue = forceRevision || now - _lastRevisionAtUtc >= TimeSpan.FromMinutes(5);
            if (revisionDue && _revisions is not null)
            {
                await _revisions.CreateRevisionAsync(current, cancellationToken);
                _lastRevisionAtUtc = now;
            }

            await _notes.UpdateAsync(new UpdateNoteRequest(
                current.Id,
                Title,
                ContentPackage,
                PlainText,
                current.FolderId,
                current.NoteType,
                IsFavorite,
                IsPinned,
                StructuredJson,
                ParseTags()), cancellationToken);

            CurrentNote = current with
            {
                Title = string.IsNullOrWhiteSpace(Title) ? "Untitled Note" : Title.Trim(),
                ContentPackage = ContentPackage,
                PlainText = PlainText,
                IsFavorite = IsFavorite,
                IsPinned = IsPinned,
                StructuredJson = string.IsNullOrWhiteSpace(StructuredJson) ? "{}" : StructuredJson,
                ModifiedAtUtc = now
            };

            _savedTagsFingerprint = CurrentTagsFingerprint();

            if (_recovery is not null)
                await _recovery.DeleteDraftAsync(current.Id, cancellationToken);

            SaveState = "Saved";
            SaveStatus = $"Saved {DateTime.Now:t}";
        }
        catch (Exception ex)
        {
            SaveState = "Error";
            SaveStatus = "Save Failed — press Ctrl+S to retry";

            if (_log is not null)
                await _log.LogAsync(
                    AppLogLevel.Error,
                    $"Failed to save note '{current.Id}'.",
                    ex,
                    cancellationToken);
        }
    }

    private bool HasChanges(Note note) =>
        !string.Equals(note.Title, string.IsNullOrWhiteSpace(Title) ? "Untitled Note" : Title.Trim(), StringComparison.Ordinal) ||
        !string.Equals(note.ContentPackage, ContentPackage, StringComparison.Ordinal) ||
        !string.Equals(note.PlainText, PlainText, StringComparison.Ordinal) ||
        note.IsFavorite != IsFavorite ||
        note.IsPinned != IsPinned ||
        !string.Equals(note.StructuredJson, string.IsNullOrWhiteSpace(StructuredJson) ? "{}" : StructuredJson, StringComparison.Ordinal) ||
        !string.Equals(_savedTagsFingerprint, CurrentTagsFingerprint(), StringComparison.Ordinal);

    private string[] ParseTags() => NormalizeTags(
        TagsText.Split(
            ',',
            StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));

    private string CurrentTagsFingerprint() =>
        string.Join("|", ParseTags().Select(value => value.ToUpperInvariant()));

    private static string[] NormalizeTags(IEnumerable<string> tags) =>
        tags
            .Select(value => string.Join(' ', (value ?? string.Empty)
                .Trim()
                .Split(' ', StringSplitOptions.RemoveEmptyEntries)))
            .Where(value => value.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
            .ToArray();

    public void Dispose()
    {
        _recoveryCoordinator?.Dispose();
        if (_ownsAutoSave) _autoSave.Dispose();
    }
}
