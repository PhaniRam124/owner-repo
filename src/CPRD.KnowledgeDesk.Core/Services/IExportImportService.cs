using CPRD.KnowledgeDesk.Core.Models;

namespace CPRD.KnowledgeDesk.Core.Services;

public interface IExportImportService
{
    Task<ExportResult> ExportAllAsync(
        string targetPath,
        CancellationToken cancellationToken);

    Task<ImportResult> ImportAsync(
        string sourcePath,
        Guid fallbackFolderId,
        CancellationToken cancellationToken);
}
