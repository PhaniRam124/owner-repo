using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public sealed record NewNoteRequest(
    string Title,
    string ContentPackage,
    string PlainText,
    Guid FolderId,
    string NoteType,
    string StructuredJson,
    IReadOnlyList<string> Tags);

public sealed record UpdateNoteRequest(
    Guid Id,
    string Title,
    string ContentPackage,
    string PlainText,
    Guid FolderId,
    string NoteType,
    bool IsFavorite,
    bool IsPinned,
    string StructuredJson,
    IReadOnlyList<string> Tags);

public interface INoteService
{
    Task<Note> CreateAsync(NewNoteRequest request, CancellationToken cancellationToken);
    Task<Note?> GetAsync(Guid id, CancellationToken cancellationToken);
    Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken);
    Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken);
}
