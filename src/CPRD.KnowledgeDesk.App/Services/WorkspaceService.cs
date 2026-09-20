namespace CPRD.KnowledgeDesk.App.Services;

public sealed class WorkspaceService
{
    public const int MaximumOpenNotes = 6;
    private readonly List<Guid> _openNoteIds = new();

    public IReadOnlyList<Guid> OpenNoteIds => _openNoteIds;

    public void Open(Guid noteId)
    {
        _openNoteIds.Remove(noteId);
        _openNoteIds.Add(noteId);

        while (_openNoteIds.Count > MaximumOpenNotes)
            _openNoteIds.RemoveAt(0);
    }

    public void Close(Guid noteId) => _openNoteIds.Remove(noteId);

    public void Clear() => _openNoteIds.Clear();
}
