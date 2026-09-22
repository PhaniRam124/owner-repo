using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using CPRD.KnowledgeDesk.Core.Models;
using CPRD.KnowledgeDesk.Core.Services;
using CPRD.KnowledgeDesk.Infrastructure.Data;

namespace CPRD.KnowledgeDesk.Infrastructure.Services;

public sealed partial class DuplicateDetectionService : IDuplicateDetectionService
{
    private readonly KnowledgeDb _db;

    public DuplicateDetectionService(KnowledgeDb db) => _db = db;

    public async Task<IReadOnlyList<DuplicateCandidate>> FindCandidatesAsync(
        DuplicateProbe probe,
        CancellationToken cancellationToken)
    {
        var probeTitle = Normalize(probe.Title);
        var probeBody = Normalize(probe.PlainText);
        var probeHash = Hash(probeBody);
        var result = new List<DuplicateCandidate>();

        await using var connection = _db.OpenConnection();
        await connection.OpenAsync(cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandText = """
            SELECT id,title,plain_text
              FROM notes
             WHERE deleted_at_utc IS NULL
               AND is_archived=0
             ORDER BY modified_at_utc DESC
            """;

        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken))
        {
            var id = Guid.Parse(reader.GetString(0));
            var title = reader.GetString(1);
            var body = reader.GetString(2);
            var normalizedTitle = Normalize(title);
            var normalizedBody = Normalize(body);

            if (probeBody.Length > 0 && normalizedBody.Length > 0 &&
                CryptographicOperations.FixedTimeEquals(probeHash, Hash(normalizedBody)))
            {
                result.Add(new DuplicateCandidate(id, title, "ExactContent", 1.0));
                continue;
            }

            if (probeTitle.Length > 0 &&
                string.Equals(probeTitle, normalizedTitle, StringComparison.Ordinal))
            {
                result.Add(new DuplicateCandidate(id, title, "ExactTitle", 1.0));
                continue;
            }

            var titleSimilarity = Similarity(probeTitle, normalizedTitle);
            var combinedSimilarity = Similarity(
                probeTitle + " " + probeBody,
                normalizedTitle + " " + normalizedBody);

            if (titleSimilarity >= 0.80 || combinedSimilarity >= 0.88)
            {
                result.Add(new DuplicateCandidate(
                    id,
                    title,
                    "HighSimilarity",
                    Math.Max(titleSimilarity, combinedSimilarity)));
            }
        }

        return result
            .OrderByDescending(candidate => candidate.Reason == "ExactContent")
            .ThenByDescending(candidate => candidate.Reason == "ExactTitle")
            .ThenByDescending(candidate => candidate.Similarity)
            .ToArray();
    }

    private static string Normalize(string? value)
    {
        if (string.IsNullOrWhiteSpace(value)) return string.Empty;
        var normalized = value.Normalize(NormalizationForm.FormKC).Trim().ToUpperInvariant();
        return WhitespaceRegex().Replace(normalized, " ");
    }

    private static byte[] Hash(string value) =>
        SHA256.HashData(Encoding.UTF8.GetBytes(value));

    private static double Similarity(string left, string right)
    {
        var a = Tokens(left);
        var b = Tokens(right);
        if (a.Count == 0 || b.Count == 0) return 0;

        var intersection = a.Intersect(b, StringComparer.Ordinal).Count();
        var union = a.Union(b, StringComparer.Ordinal).Count();
        var jaccard = union == 0 ? 0 : (double)intersection / union;

        // The overlap coefficient intentionally catches a stable title with
        // a harmless suffix such as a year ("FortiGate Renewal" vs
        // "FortiGate Renewal 2026") without ever auto-merging content.
        var overlap = (double)intersection / Math.Min(a.Count, b.Count);
        return Math.Max(jaccard, overlap);
    }

    private static HashSet<string> Tokens(string value) =>
        TokenRegex().Matches(value)
            .Select(match => match.Value)
            .Where(token => token.Length > 0)
            .ToHashSet(StringComparer.Ordinal);

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRegex();

    [GeneratedRegex(@"[\p{L}\p{N}]+")]
    private static partial Regex TokenRegex();
}
