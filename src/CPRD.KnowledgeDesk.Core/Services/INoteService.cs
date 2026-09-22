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

    Task<IReadOnlyList<string>> GetTagsAsync(Guid id, CancellationToken cancellationToken) =>
        Task.FromResult<IReadOnlyList<string>>(Array.Empty<string>());

    Task UpdateAsync(UpdateNoteRequest request, CancellationToken cancellationToken);
    Task<IReadOnlyList<Note>> ListByFolderAsync(Guid folderId, CancellationToken cancellationToken);
    Task MarkOpenedAsync(Guid id, DateTimeOffset openedAtUtc, CancellationToken cancellationToken);

    Task ArchiveAsync(Guid id, CancellationToken cancellationToken);
    Task UnarchiveAsync(Guid id, CancellationToken cancellationToken);
    Task MoveToTrashAsync(Guid id, CancellationToken cancellationToken);
    Task RestoreFromTrashAsync(Guid id, CancellationToken cancellationToken);
    Task DeletePermanentlyAsync(Guid id, CancellationToken cancellationToken);
    Task<int> PurgeTrashOlderThanAsync(DateTimeOffset cutoffUtc, CancellationToken cancellationToken);
    Task<IReadOnlyList<Note>> ListTrashAsync(CancellationToken cancellationToken);
}
