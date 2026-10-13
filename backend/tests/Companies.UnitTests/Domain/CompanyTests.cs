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

        Assert.Multiple(() =>
        {
            Assert.That(company.Id, Is.Not.EqualTo(Guid.Empty));
            Assert.That(company.Id.Version, Is.EqualTo(7));
            Assert.That(company.Name, Is.EqualTo(Name));
            Assert.That(company.Website, Is.EqualTo(Website));
            Assert.That(company.CreatedAt, Is.EqualTo(Now));
        });
    }

    [Test]
    public void Create_TwiceAtSameInstant_GeneratesDistinctIds()
    {
        var clock = new FakeTimeProvider(Now);

        var first = Company.Create(Name, Website, clock);
        var second = Company.Create(Name, Website, clock);

        Assert.That(first.Id, Is.Not.EqualTo(second.Id));
    }

    [Test]
    public void Restore_RebuildsExistingCompany()
    {
        var id = Guid.CreateVersion7();

        var company = Company.Restore(id, Name, Website, Now);

        Assert.Multiple(() =>
        {
            Assert.That(company.Id, Is.EqualTo(id));
            Assert.That(company.CreatedAt, Is.EqualTo(Now));
        });
    }

    [Test]
    public void Restore_WithEmptyId_Throws()
    {
        Assert.That(() => Company.Restore(Guid.Empty, Name, Website, Now), Throws.ArgumentException);
    }
}
