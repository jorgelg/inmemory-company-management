using Companies.Application.Companies;
using Companies.Domain.Companies;
using Companies.Infrastructure.Persistence;

namespace Companies.UnitTests.Infrastructure;

[TestFixture]
public sealed class InMemoryCompanyRepositoryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private InMemoryCompanyRepository _repository = null!;

    [SetUp]
    public void SetUp() => _repository = new InMemoryCompanyRepository();

    [Test]
    public async Task TryAdd_ThenGetById_ReturnsEquivalentCompany()
    {
        var company = Make("Verus LLC", "https://www.verus.com");

        (await _repository.TryAddAsync(company, CancellationToken.None)).ShouldBeTrue();
        var stored = await _repository.GetByIdAsync(company.Id, CancellationToken.None);

        stored.ShouldNotBeNull().ShouldSatisfyAllConditions(
            s => s.ShouldNotBeSameAs(company),
            s => s.Id.ShouldBe(company.Id),
            s => s.Name.ShouldBe(company.Name),
            s => s.Website.ShouldBe(company.Website),
            s => s.CreatedAt.ShouldBe(company.CreatedAt));
    }

    [Test]
    public async Task GetById_Unknown_ReturnsNull()
    {
        (await _repository.GetByIdAsync(Guid.CreateVersion7(), CancellationToken.None)).ShouldBeNull();
    }

    [Test]
    public async Task TryAdd_SameRegistrableDomain_IsRejected()
    {
        await _repository.TryAddAsync(Make("Acme", "https://www.acme.com"), CancellationToken.None);

        var added = await _repository.TryAddAsync(Make("Acme Shop", "https://shop.acme.com"), CancellationToken.None);

        added.ShouldBeFalse();
        (await _repository.ListAsync(CompanyFilter.None, CancellationToken.None)).Count.ShouldBe(1);
    }

    [Test]
    public async Task TryAdd_ConcurrentSameDomain_ExactlyOneSucceeds()
    {
        var attempts = Enumerable.Range(0, 50)
            .Select(i => Task.Run(() => _repository.TryAddAsync(Make($"Acme {i}", "https://acme.com"), CancellationToken.None)));

        var results = await Task.WhenAll(attempts);

        results.Count(added => added).ShouldBe(1);
    }

    [Test]
    public async Task ExistsByDomain_ReflectsStoredDomains()
    {
        await _repository.TryAddAsync(Make("Acme", "https://www.acme.co.uk"), CancellationToken.None);

        (await _repository.ExistsByDomainAsync("acme.co.uk", CancellationToken.None)).ShouldBeTrue();
        (await _repository.ExistsByDomainAsync("acme.com", CancellationToken.None)).ShouldBeFalse();
    }

    [Test]
    public async Task List_WithoutFilter_ReturnsAll()
    {
        await Seed();

        var all = await _repository.ListAsync(CompanyFilter.None, CancellationToken.None);

        all.Select(c => c.Name.Value).ShouldBe(["Acme Rockets", "Blue Bottle Coffee", "Acme Bakery"], ignoreOrder: true);
    }

    [TestCase("acme", new[] { "Acme Rockets", "Acme Bakery" })]
    [TestCase("BOTTLE", new[] { "Blue Bottle Coffee" })]
    [TestCase("Blue Bottle Coffee", new[] { "Blue Bottle Coffee" })]
    [TestCase("zzz", new string[0])]
    public async Task List_NameContains_IsCaseInsensitive(string fragment, string[] expected)
    {
        await Seed();

        var matches = await _repository.ListAsync(new CompanyFilter(NameContains: fragment), CancellationToken.None);

        matches.Select(c => c.Name.Value).ShouldBe(expected, ignoreOrder: true);
    }

    [Test]
    public async Task List_RegistrableDomain_MatchesExactly()
    {
        await Seed();

        var matches = await _repository.ListAsync(new CompanyFilter(RegistrableDomain: "acmerockets.com"), CancellationToken.None);

        matches.ShouldHaveSingleItem().Name.Value.ShouldBe("Acme Rockets");
    }

    [Test]
    public async Task List_DomainContains_MatchesHostFragment()
    {
        await Seed();

        var matches = await _repository.ListAsync(new CompanyFilter(DomainContains: "SHOP.acme"), CancellationToken.None);

        matches.ShouldHaveSingleItem().Name.Value.ShouldBe("Acme Bakery");
    }

    [Test]
    public async Task List_CombinedFilters_MustAllMatch()
    {
        await Seed();

        var matches = await _repository.ListAsync(
            new CompanyFilter(NameContains: "acme", DomainContains: "rockets"), CancellationToken.None);

        matches.ShouldHaveSingleItem().Name.Value.ShouldBe("Acme Rockets");
    }

    [Test]
    public async Task Operations_WhenCancelled_Throw()
    {
        using var cancelled = new CancellationTokenSource();
        await cancelled.CancelAsync();

        await Should.ThrowAsync<OperationCanceledException>(() => _repository.ListAsync(CompanyFilter.None, cancelled.Token));
        await Should.ThrowAsync<OperationCanceledException>(
            () => _repository.TryAddAsync(Make("Acme", "https://acme.com"), cancelled.Token));
    }

    private async Task Seed()
    {
        await _repository.TryAddAsync(Make("Acme Rockets", "https://www.acmerockets.com"), CancellationToken.None);
        await _repository.TryAddAsync(Make("Blue Bottle Coffee", "https://bluebottlecoffee.com"), CancellationToken.None);
        await _repository.TryAddAsync(Make("Acme Bakery", "https://shop.acme-bakery.com"), CancellationToken.None);
    }

    private static Company Make(string name, string website) =>
        Company.Restore(Guid.CreateVersion7(), CompanyName.Create(name).Value, WebsiteUrl.Create(website).Value, Now);
}
