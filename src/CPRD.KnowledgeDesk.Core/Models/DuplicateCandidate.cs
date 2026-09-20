namespace CPRD.KnowledgeDesk.Core.Models;

public sealed record DuplicateProbe(string Title, string PlainText);

public sealed record DuplicateCandidate(
    Guid NoteId,
    string Title,
    string Reason,
    double Similarity);
