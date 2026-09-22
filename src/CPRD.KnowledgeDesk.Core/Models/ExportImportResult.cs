namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record ExportResult(
    string Path,
    int NoteCount);

public sealed record ImportResult(
    int ImportedCount,
    int SkippedDuplicateCount);
