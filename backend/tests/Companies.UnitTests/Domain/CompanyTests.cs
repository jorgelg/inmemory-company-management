using Companies.Domain.Companies;
using Microsoft.Extensions.Time.Testing;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class CompanyTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private static readonly CompanyName Name = CompanyName.Create("Verus LLC").Value;
    private static readonly WebsiteUrl Website = WebsiteUrl.Create("https://www.verus.com").Value;

    [Test]
    public void Create_AssignsVersion7IdAndTimestamp()
    {
        var company = Company.Create(Name, Website, new FakeTimeProvider(Now));

        company.ShouldSatisfyAllConditions(
            c => c.Id.ShouldNotBe(Guid.Empty),
            c => c.Id.Version.ShouldBe(7),
            c => c.Name.ShouldBe(Name),
            c => c.Website.ShouldBe(Website),
            c => c.CreatedAt.ShouldBe(Now));
    }

    [Test]
    public void Create_TwiceAtSameInstant_GeneratesDistinctIds()
    {
        var clock = new FakeTimeProvider(Now);

        var first = Company.Create(Name, Website, clock);
        var second = Company.Create(Name, Website, clock);

        first.Id.ShouldNotBe(second.Id);
    }

    [Test]
    public void Restore_RebuildsExistingCompany()
    {
        var id = Guid.CreateVersion7();

        var company = Company.Restore(id, Name, Website, Now);

        company.ShouldSatisfyAllConditions(
            c => c.Id.ShouldBe(id),
            c => c.CreatedAt.ShouldBe(Now));
    }

    [Test]
    public void Restore_WithEmptyId_Throws()
    {
        Should.Throw<ArgumentException>(() => Company.Restore(Guid.Empty, Name, Website, Now));
    }
}
