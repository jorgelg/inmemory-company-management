using Companies.Application.Text;
using Companies.Domain.Companies;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Companies.Application.Relevance;

/// <summary>
/// Runs the registered <see cref="IRelevanceStrategy"/> instances in order and stops at the first
/// whose score reaches <see cref="RelevanceOptions.Threshold"/>. The verdict carries the best evidence
/// so a rejection can explain itself.
/// </summary>
internal sealed class RelevanceValidator(
    IEnumerable<IRelevanceStrategy> strategies,
    IOptions<RelevanceOptions> options,
    ILogger<RelevanceValidator> logger) : IRelevanceValidator
{
    private readonly IRelevanceStrategy[] _strategies = [.. strategies];

    public async Task<RelevanceVerdict> EvaluateAsync(CompanyName name, WebsiteUrl website, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentNullException.ThrowIfNull(website);

        if (_strategies.Length == 0)
        {
            throw new InvalidOperationException("No relevance strategies are registered.");
        }

        var threshold = options.Value.Threshold;
        var candidate = new RelevanceCandidate(name, website, CompanyNameNormalizer.Normalize(name));
        var evidence = new List<RelevanceEvidence>(_strategies.Length);

        foreach (var strategy in _strategies)
        {
            var result = await strategy.EvaluateAsync(candidate, cancellationToken).ConfigureAwait(false);
            evidence.Add(result);

            if (result.Score >= threshold)
            {
                break;
            }
        }

        var best = evidence.MaxBy(e => e.Score)!;
        var verdict = new RelevanceVerdict(best.Score >= threshold, threshold, best, evidence);

        logger.RelevanceEvaluated(name.Value, website.RegistrableDomain, verdict.IsRelevant, best.Score, best.Strategy, best.Reason);

        return verdict;
    }
}

internal static partial class RelevanceValidatorLog
{
    [LoggerMessage(Level = LogLevel.Information,
        Message = "Relevance of '{CompanyName}' to {Domain}: relevant={IsRelevant}, score={Score:0.00} via {Strategy} ({Reason})")]
    public static partial void RelevanceEvaluated(
        this ILogger logger, string companyName, string domain, bool isRelevant, double score, string strategy, string reason);
}
