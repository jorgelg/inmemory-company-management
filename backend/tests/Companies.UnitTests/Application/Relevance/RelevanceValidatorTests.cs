using Companies.Application.Relevance;
using Companies.Domain.Companies;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;

namespace Companies.UnitTests.Application.Relevance;

[TestFixture]
public sealed class RelevanceValidatorTests
{
    private static readonly CompanyName Name = CompanyName.Create("Alphabet Inc.").Value;
    private static readonly WebsiteUrl Website = WebsiteUrl.Create("https://abc.xyz").Value;

    [Test]
    public async Task Evaluate_WhenFirstStrategyPasses_DoesNotRunLaterStrategies()
    {
        var first = Strategy("first", 0.9);
        var second = Strategy("second", 0.75);

        var verdict = await CreateValidator(first, second).EvaluateAsync(Name, Website, CancellationToken.None);

        verdict.ShouldSatisfyAllConditions(
            v => v.IsRelevant.ShouldBeTrue(),
            v => v.Best.Strategy.ShouldBe("first"),
            v => v.Evidence.Count.ShouldBe(1));
        await second.DidNotReceiveWithAnyArgs().EvaluateAsync(default!, default);
    }

    [Test]
    public async Task Evaluate_WhenFirstStrategyFails_FallsBackToNextStrategy()
    {
        var verdict = await CreateValidator(Strategy("domain", 0.2), Strategy("content", 0.75))
            .EvaluateAsync(Name, Website, CancellationToken.None);

        verdict.ShouldSatisfyAllConditions(
            v => v.IsRelevant.ShouldBeTrue(),
            v => v.Best.Strategy.ShouldBe("content"),
            v => v.Evidence.Select(e => e.Strategy).ShouldBe(["domain", "content"]));
    }

    [Test]
    public async Task Evaluate_WhenAllStrategiesFail_ReportsBestEvidence()
    {
        var verdict = await CreateValidator(Strategy("domain", 0.4), Strategy("content", 0.2))
            .EvaluateAsync(Name, Website, CancellationToken.None);

        verdict.ShouldSatisfyAllConditions(
            v => v.IsRelevant.ShouldBeFalse(),
            v => v.Best.Strategy.ShouldBe("domain"),
            v => v.Best.Score.ShouldBe(0.4),
            v => v.Threshold.ShouldBe(0.6));
    }

    [Test]
    public async Task Evaluate_ScoreEqualToThreshold_IsRelevant()
    {
        var verdict = await CreateValidator(Strategy("domain", 0.6)).EvaluateAsync(Name, Website, CancellationToken.None);

        verdict.IsRelevant.ShouldBeTrue();
    }

    [Test]
    public async Task Evaluate_PassesNormalizedNameToStrategies()
    {
        var strategy = Strategy("domain", 1.0);

        await CreateValidator(strategy).EvaluateAsync(Name, Website, CancellationToken.None);

        await strategy.Received(1).EvaluateAsync(
            Arg.Is<RelevanceCandidate>(c => c.Name == Name && c.Website == Website && c.NormalizedName.Compact == "alphabet"),
            Arg.Any<CancellationToken>());
    }

    private static IRelevanceStrategy Strategy(string name, double score)
    {
        var strategy = Substitute.For<IRelevanceStrategy>();
        strategy.EvaluateAsync(Arg.Any<RelevanceCandidate>(), Arg.Any<CancellationToken>())
            .Returns(new RelevanceEvidence(name, score, $"{name} reason"));
        return strategy;
    }

    private static RelevanceValidator CreateValidator(params IRelevanceStrategy[] strategies) =>
        new(strategies, Options.Create(new RelevanceOptions { Threshold = 0.6 }), NullLogger<RelevanceValidator>.Instance);
}
