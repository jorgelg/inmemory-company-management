using Companies.Application.Relevance;
using static Companies.UnitTests.Application.Relevance.RelevanceTestData;

namespace Companies.UnitTests.Application.Relevance;

[TestFixture]
public sealed class DomainMatchStrategyTests
{
    private readonly DomainMatchStrategy _strategy = new();

    [TestCase("Verus LLC", "https://www.verus.com")]
    [TestCase("Acme Inc.", "https://acme.co.uk")]
    [TestCase("Müller GmbH", "https://www.müller.de")]
    [TestCase("AT&T Inc.", "https://www.att.com")]
    [TestCase("Blue Bottle Coffee", "https://blue-bottle-coffee.com")]
    [TestCase("Smith & Jones Law", "https://smithandjoneslaw.com")]
    [TestCase("Bank of America", "https://www.bankofamerica.com")]
    [TestCase("Acme", "https://acmecorp.com")]
    [TestCase("Tesla Motors", "https://www.tesla.com")]
    [TestCase("Blue Bottle Coffee", "https://bluebottle.com")]
    [TestCase("International Business Machines", "https://www.ibm.com")]
    [TestCase("Blue Bottle Coffee", "https://bluebottleroasters.com")]
    [TestCase("Gogle", "https://google.com")]
    [TestCase("Google", "https://www.google.com.bo")]
    [TestCase("Ministerio de Salud", "https://www.ministeriodesalud.gob.bo")]
    public async Task Evaluate_RelatedNameAndDomain_ScoresAtLeastThreshold(string name, string website)
    {
        var evidence = await _strategy.EvaluateAsync(Candidate(name, website), CancellationToken.None);

        evidence.Score.ShouldBeGreaterThanOrEqualTo(Threshold, evidence.Reason);
    }

    [TestCase("Acme", "https://www.example.com")]
    [TestCase("Verus LLC", "https://virus.com")]
    [TestCase("Microsoft", "https://www.apple.com")]
    [TestCase("Alphabet Inc.", "https://abc.xyz")]
    [TestCase("Acme Widgets", "https://widgetsworld.com")]
    public async Task Evaluate_UnrelatedNameAndDomain_ScoresBelowThreshold(string name, string website)
    {
        var evidence = await _strategy.EvaluateAsync(Candidate(name, website), CancellationToken.None);

        evidence.Score.ShouldBeLessThan(Threshold, evidence.Reason);
    }

    [TestCase("Verus LLC", "https://www.verus.com", 1.0, "exactly")]
    [TestCase("Smith & Jones Law", "https://smithandjoneslaw.com", 0.9, "every word")]
    [TestCase("Acme", "https://acmecorp.com", 0.85, "overlap")]
    [TestCase("International Business Machines", "https://www.ibm.com", 0.8, "acronym")]
    public async Task Evaluate_ReportsStrongestSignal(string name, string website, double score, string reasonFragment)
    {
        var evidence = await _strategy.EvaluateAsync(Candidate(name, website), CancellationToken.None);

        evidence.ShouldSatisfyAllConditions(
            e => e.Strategy.ShouldBe(DomainMatchStrategy.StrategyName),
            e => e.Score.ShouldBe(score, tolerance: 0.0001),
            e => e.Reason.ShouldContain(reasonFragment, Case.Insensitive));
    }
}
