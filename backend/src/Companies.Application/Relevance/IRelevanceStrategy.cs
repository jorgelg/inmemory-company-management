namespace Companies.Application.Relevance;

/// <summary>
/// One way of judging whether a company name belongs to a website. Strategies run in
/// registration order, and evaluation stops at the first that reaches the threshold, so cheap
/// offline strategies are registered before expensive ones.
/// </summary>
public interface IRelevanceStrategy
{
    string Name { get; }

    Task<RelevanceEvidence> EvaluateAsync(RelevanceCandidate candidate, CancellationToken cancellationToken);
}
