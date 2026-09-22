namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record BackupInfo(
    string Path,
    DateTimeOffset CreatedAtUtc,
    long SizeBytes,
    bool IsAutomatic);
