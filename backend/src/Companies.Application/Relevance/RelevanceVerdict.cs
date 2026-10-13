namespace Companies.Application.Relevance;

/// <summary>The overall decision, with the best evidence and every strategy that ran, in order.</summary>
public sealed record RelevanceVerdict(
    bool IsRelevant,
    double Threshold,
    RelevanceEvidence Best,
    IReadOnlyList<RelevanceEvidence> Evidence);
