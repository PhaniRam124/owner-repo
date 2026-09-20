using System.Security.Cryptography;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;
using CPRD.KnowledgeDesk.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed class AttachmentService : IAttachmentService
{
    private readonly AttachmentRepository _attachments;
    private readonly SearchRepository _search;
    private readonly KnowledgeDb _db;
    private readonly IAppPaths _paths;

    public AttachmentService(
        AttachmentRepository attachments,
        SearchRepository search,
        KnowledgeDb db,
        IAppPaths paths)
    {
        _attachments = attachments;
        _search = search;
        _db = db;
        _paths = paths;
    }

    public async Task<Attachment> AddAsync(Guid noteId, string sourcePath, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
            throw new ArgumentException("Source path is required.", nameof(sourcePath));
        if (!File.Exists(sourcePath))
            throw new FileNotFoundException("Attachment source file was not found.", sourcePath);

        var id = Guid.NewGuid();
        var filename = Path.GetFileName(sourcePath);
        var extension = SafeExtension(filename);
        var noteDirectory = Path.Combine(_paths.AttachmentsDirectory, noteId.ToString("D"));
        Directory.CreateDirectory(noteDirectory);

        var tempPath = Path.Combine(noteDirectory, id.ToString("N") + ".tmp");
        var finalPath = Path.Combine(noteDirectory, id.ToString("N") + extension);

        try
        {
            await using (var input = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            await using (var output = new FileStream(tempPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                await input.CopyToAsync(output, cancellationToken);

            var sourceHash = await HashFileAsync(sourcePath, cancellationToken);
            var copiedHash = await HashFileAsync(tempPath, cancellationToken);
            if (!CryptographicOperations.FixedTimeEquals(sourceHash, copiedHash))
                throw new IOException("Attachment verification failed: copied file hash does not match the source.");

            File.Move(tempPath, finalPath);

            var attachment = new Attachment(
                id,
                noteId,
                filename,
                finalPath,
                GetMimeType(extension),
                new FileInfo(finalPath).Length,
                Convert.ToHexString(copiedHash),
                DateTimeOffset.UtcNow);

            await using var connection = _db.OpenConnection();
            await connection.OpenAsync(cancellationToken);
            await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
            try
            {
                await _attachments.InsertAsync(connection, transaction, attachment, cancellationToken);
                await _search.RefreshAsync(connection, transaction, noteId, cancellationToken);
                await transaction.CommitAsync(cancellationToken);
                return attachment;
            }
            catch
            {
                await transaction.RollbackAsync(cancellationToken);
                if (File.Exists(finalPath)) File.Delete(finalPath);
                throw;
            }
        }
        finally
        {
            if (File.Exists(tempPath)) File.Delete(tempPath);
        }
    }

    public Task<IReadOnlyList<Attachment>> ListAsync(Guid noteId, CancellationToken cancellationToken) =>
        _attachments.ListAsync(noteId, cancellationToken);

    public async Task RemoveAsync(Guid attachmentId, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.GetAsync(attachmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attachment '{attachmentId}' was not found.");

        var quarantine = attachment.StoredPath + ".delete";
        if (File.Exists(quarantine)) File.Delete(quarantine);
        if (File.Exists(attachment.StoredPath)) File.Move(attachment.StoredPath, quarantine);

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var transaction = (SqliteTransaction)await connection.BeginTransactionAsync(cancellationToken);
        try
        {
            await _attachments.DeleteAsync(connection, transaction, attachmentId, cancellationToken);
            await _search.RefreshAsync(connection, transaction, attachment.NoteId, cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            if (File.Exists(quarantine)) File.Delete(quarantine);
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            if (File.Exists(quarantine) && !File.Exists(attachment.StoredPath))
                File.Move(quarantine, attachment.StoredPath);
            throw;
        }
    }

    public async Task SaveAsAsync(Guid attachmentId, string destinationPath, CancellationToken cancellationToken)
    {
        var attachment = await _attachments.GetAsync(attachmentId, cancellationToken)
            ?? throw new KeyNotFoundException($"Attachment '{attachmentId}' was not found.");
        if (!File.Exists(attachment.StoredPath))
            throw new FileNotFoundException("Managed attachment file is missing.", attachment.StoredPath);

        var directory = Path.GetDirectoryName(destinationPath);
        if (!string.IsNullOrEmpty(directory)) Directory.CreateDirectory(directory);

        await using var input = new FileStream(attachment.StoredPath, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        await using var output = new FileStream(destinationPath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
        await input.CopyToAsync(output, cancellationToken);
    }

    private static async Task<byte[]> HashFileAsync(string path, CancellationToken cancellationToken)
    {
        await using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true);
        return await SHA256.HashDataAsync(stream, cancellationToken);
    }

    private static string SafeExtension(string filename)
    {
        var extension = Path.GetExtension(filename);
        if (extension.Length > 16 || extension.Any(ch => !char.IsLetterOrDigit(ch) && ch != '.'))
            return string.Empty;
        return extension.ToLowerInvariant();
    }

    private static string GetMimeType(string extension) => extension switch
    {
        ".pdf" => "application/pdf",
        ".png" => "image/png",
        ".jpg" or ".jpeg" => "image/jpeg",
        ".gif" => "image/gif",
        ".txt" => "text/plain",
        ".md" => "text/markdown",
        ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".xlsx" => "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        _ => "application/octet-stream"
    };
}
