using Companies.Application.Relevance;
using Companies.Domain.Companies;
using Microsoft.Extensions.Options;
using NSubstitute;
using static Companies.UnitTests.Application.Relevance.RelevanceTestData;

namespace Companies.UnitTests.Application.Relevance;

[TestFixture]
public sealed class ContentInspectionStrategyTests
{
    private IWebsiteContentFetcher _fetcher = null!;

    [SetUp]
    public void SetUp() => _fetcher = Substitute.For<IWebsiteContentFetcher>();

    [Test]
    public async Task Evaluate_WhenDisabled_SkipsWithoutFetching()
    {
        var strategy = CreateStrategy(enabled: false);

        var evidence = await strategy.EvaluateAsync(Candidate("Alphabet Inc.", "https://abc.xyz"), CancellationToken.None);

        evidence.Score.ShouldBe(0);
        await _fetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Test]
    public async Task Evaluate_WhenTitleMentionsName_ScoresAboveThreshold()
    {
        ReturnContent(new WebsiteContent(Title: "Alphabet", SiteName: null, Heading: null));

        var evidence = await CreateStrategy().EvaluateAsync(Candidate("Alphabet Inc.", "https://abc.xyz"), CancellationToken.None);

        evidence.ShouldSatisfyAllConditions(
            e => e.Strategy.ShouldBe(ContentInspectionStrategy.StrategyName),
            e => e.Score.ShouldBe(0.75, tolerance: 0.0001),
            e => e.Score.ShouldBeGreaterThanOrEqualTo(Threshold));
    }

    [Test]
    public async Task Evaluate_WhenSiteNameMentionsName_ScoresAboveThreshold()
    {
        ReturnContent(new WebsiteContent(Title: "Home", SiteName: "Blue Bottle Coffee", Heading: null));

        var evidence = await CreateStrategy().EvaluateAsync(
            Candidate("Blue Bottle Coffee Company", "https://www.example-roasters.com"), CancellationToken.None);

        evidence.Score.ShouldBeGreaterThanOrEqualTo(Threshold);
    }

    [Test]
    public async Task Evaluate_WhenContentMentionsSomeWords_ScoresProportionallyBelowThreshold()
    {
        ReturnContent(new WebsiteContent(Title: "Blue Ocean Travel", SiteName: null, Heading: null));

        var evidence = await CreateStrategy().EvaluateAsync(
            Candidate("Blue Bottle Coffee", "https://www.example.com"), CancellationToken.None);

        evidence.Score.ShouldBe(0.2, tolerance: 0.0001);
    }

    [Test]
    public async Task Evaluate_WhenContentUnavailable_ScoresZero()
    {
        ReturnContent(null);

        var evidence = await CreateStrategy().EvaluateAsync(Candidate("Alphabet Inc.", "https://abc.xyz"), CancellationToken.None);

        evidence.ShouldSatisfyAllConditions(
            e => e.Score.ShouldBe(0),
            e => e.Reason.ShouldContain("could not be retrieved"));
    }

    private ContentInspectionStrategy CreateStrategy(bool enabled = true) =>
        new(_fetcher, Options.Create(new RelevanceOptions { ContentInspection = { Enabled = enabled } }));

    private void ReturnContent(WebsiteContent? content) =>
        _fetcher.FetchAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>()).Returns(content);
}
