using Companies.Application.Search;
using Companies.Domain.Companies;

namespace Companies.UnitTests.Application.Search;

[TestFixture]
public sealed class CompanySearchRankerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private readonly CompanySearchRanker _ranker = new();

    [Test]
    public void Rank_OrdersByRelevanceAndDropsNonMatches()
    {
        Company[] companies =
        [
            Make("Microsoft", "https://microsoft.com"),
            Make("Bluebird Logistics", "https://verusbirds.com"),
            Make("Acme Verus Partners", "https://acmepartners.com"),
            Make("Veruscorp Holdings", "https://veruscorp.com"),
            Make("Verus LLC", "https://verus.com"),
        ];

        var ranked = _ranker.Rank(companies, "verus");

        ranked.Select(r => r.Company.Name.Value).ShouldBe(
            ["Verus LLC", "Veruscorp Holdings", "Acme Verus Partners", "Bluebird Logistics"]);
        ranked.Select(r => r.Score).ShouldBe([1.0, 0.8, 0.7, 0.4]);
    }

    [Test]
    public void Rank_IsCaseAndDiacriticInsensitive()
    {
        var ranked = _ranker.Rank([Make("Müller GmbH", "https://mueller.de")], "MULLER");

        ranked.ShouldHaveSingleItem().Score.ShouldBe(1.0);
    }

    [Test]
    public void Rank_MatchesTyposFuzzily()
    {
        var ranked = _ranker.Rank([Make("Verus LLC", "https://verus.com")], "veruz");

        ranked.ShouldHaveSingleItem().Score.ShouldBe(0.3);
    }

    [Test]
    public void Rank_MultiWordTerm_MatchesWordPrefixes()
    {
        var ranked = _ranker.Rank([Make("Blue Bottle Coffee", "https://bluebottlecoffee.com")], "blue cof");

        ranked.ShouldHaveSingleItem().Score.ShouldBe(0.6);
    }

    [Test]
    public void Rank_EqualScores_AreOrderedByName()
    {
        var ranked = _ranker.Rank(
            [Make("Zeta Acme", "https://zeta.com"), Make("Alpha Acme", "https://alpha.com")], "acme");

        ranked.Select(r => r.Company.Name.Value).ShouldBe(["Alpha Acme", "Zeta Acme"]);
    }

    [Test]
    public void Rank_NoMatches_ReturnsEmpty()
    {
        _ranker.Rank([Make("Microsoft", "https://microsoft.com")], "verus").ShouldBeEmpty();
    }

    private static Company Make(string name, string website) =>
        Company.Restore(Guid.CreateVersion7(), CompanyName.Create(name).Value, WebsiteUrl.Create(website).Value, Now);
}
