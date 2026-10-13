namespace Companies.Application.Relevance;

/// <summary>One strategy's finding: a score in [0, 1] and a short, human-readable reason.</summary>
public sealed record RelevanceEvidence(string Strategy, double Score, string Reason);
