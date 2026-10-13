using Companies.Application.Companies;
using Companies.Application.Companies.Contracts;
using Companies.Application.Relevance;
using Companies.Application.Search;
using Companies.Domain.Common;
using Companies.Domain.Companies;
using Companies.UnitTests.Support;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Time.Testing;
using NSubstitute;

namespace Companies.UnitTests.Application.Companies;

[TestFixture]
public sealed class CompanyServiceQueryTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 13, 12, 0, 0, TimeSpan.Zero);

    private ICompanyRepository _repository = null!;
    private CompanyService _service = null!;

    [SetUp]
    public void SetUp()
    {
        _repository = Substitute.For<ICompanyRepository>();
        _service = new CompanyService(
            _repository,
            Substitute.For<IRelevanceValidator>(),
            new CompanySearchRanker(),
            new FakeTimeProvider(Now),
            NullLogger<CompanyService>.Instance);
    }

    [Test]
    public async Task GetById_Existing_ReturnsCompany()
    {
        var company = Make("Verus LLC", "https://www.verus.com");
        _repository.GetByIdAsync(company.Id, Arg.Any<CancellationToken>()).Returns(company);

        var result = await _service.GetByIdAsync(company.Id, CancellationToken.None);

        result.ShouldBeSuccess().ShouldSatisfyAllConditions(
            r => r.Id.ShouldBe(company.Id),
            r => r.Name.ShouldBe("Verus LLC"),
            r => r.Domain.ShouldBe("verus.com"));
    }

    [Test]
    public async Task GetById_Missing_FailsWithNotFound()
    {
        var id = Guid.CreateVersion7();

        var result = await _service.GetByIdAsync(id, CancellationToken.None);

        var error = result.ShouldBeFailure();
        error.ShouldSatisfyAllConditions(
            e => e.Type.ShouldBe(ErrorType.NotFound),
            e => e.Code.ShouldBe("company.not_found"),
            e => e.Message.ShouldContain(id.ToString()));
    }

    [Test]
    public async Task Search_Defaults_ReturnsFirstPageSortedByName()
    {
        GivenStored(Make("Zeta", "https://zeta.com"), Make("acme", "https://acme.com"), Make("Beta", "https://beta.com"));

        var page = (await _service.SearchAsync(new CompanyQuery(), CancellationToken.None)).ShouldBeSuccess();

        page.ShouldSatisfyAllConditions(
            p => p.Items.Select(i => i.Name).ShouldBe(["acme", "Beta", "Zeta"]),
            p => p.Page.ShouldBe(1),
            p => p.PageSize.ShouldBe(CompanyQuery.DefaultPageSize),
            p => p.TotalCount.ShouldBe(3),
            p => p.TotalPages.ShouldBe(1));
        await _repository.Received(1).ListAsync(CompanyFilter.None, Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Search_EmptyStore_ReturnsEmptyPage()
    {
        GivenStored();

        var page = (await _service.SearchAsync(new CompanyQuery { Name = "nothing" }, CancellationToken.None)).ShouldBeSuccess();

        page.ShouldSatisfyAllConditions(
            p => p.Items.ShouldBeEmpty(),
            p => p.TotalCount.ShouldBe(0),
            p => p.TotalPages.ShouldBe(0));
    }

    [Test]
    public async Task Search_NameFilter_IsTrimmedAndPassedToRepository()
    {
        GivenStored();

        await _service.SearchAsync(new CompanyQuery { Name = "  acme " }, CancellationToken.None);

        await _repository.Received(1).ListAsync(new CompanyFilter(NameContains: "acme"), Arg.Any<CancellationToken>());
    }

    [TestCase("acme.com", "acme.com")]
    [TestCase("www.Acme.com", "acme.com")]
    [TestCase("https://shop.acme.co.uk/products?x=1", "acme.co.uk")]
    [TestCase("acme.com/about", "acme.com")]
    public async Task Search_DomainFilter_WithFullDomain_MatchesRegistrableDomain(string input, string registrable)
    {
        GivenStored();

        await _service.SearchAsync(new CompanyQuery { Domain = input }, CancellationToken.None);

        await _repository.Received(1).ListAsync(new CompanyFilter(RegistrableDomain: registrable), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Search_DomainFilter_WithFragment_MatchesDomainContaining()
    {
        GivenStored();

        await _service.SearchAsync(new CompanyQuery { Domain = " ACME " }, CancellationToken.None);

        await _repository.Received(1).ListAsync(new CompanyFilter(DomainContains: "acme"), Arg.Any<CancellationToken>());
    }

    [Test]
    public async Task Search_WithTerm_OrdersByRelevanceAndIncludesScore()
    {
        GivenStored(
            Make("Acme Verus Partners", "https://acmepartners.com"),
            Make("Microsoft", "https://microsoft.com"),
            Make("Verus LLC", "https://verus.com"));

        var page = (await _service.SearchAsync(new CompanyQuery { Search = "verus" }, CancellationToken.None)).ShouldBeSuccess();

        page.ShouldSatisfyAllConditions(
            p => p.Items.Select(i => i.Name).ShouldBe(["Verus LLC", "Acme Verus Partners"]),
            p => p.Items.Select(i => i.RelevanceScore).ShouldBe([1.0, 0.7]),
            p => p.TotalCount.ShouldBe(2));
    }

    [Test]
    public async Task Search_WithTermAndExplicitSort_SortsButKeepsScores()
    {
        GivenStored(Make("Verus LLC", "https://verus.com"), Make("Acme Verus Partners", "https://acmepartners.com"));

        var page = (await _service.SearchAsync(new CompanyQuery { Search = "verus", Sort = "name" }, CancellationToken.None))
            .ShouldBeSuccess();

        page.Items.Select(i => (i.Name, i.RelevanceScore)).ShouldBe([("Acme Verus Partners", 0.7), ("Verus LLC", 1.0)]);
    }

    [Test]
    public async Task Search_SortByCreatedAtDescending_ReturnsNewestFirst()
    {
        GivenStored(
            Make("Old", "https://old.com", Now.AddDays(-2)),
            Make("New", "https://new.com", Now),
            Make("Mid", "https://mid.com", Now.AddDays(-1)));

        var page = (await _service.SearchAsync(new CompanyQuery { Sort = "-createdAt" }, CancellationToken.None)).ShouldBeSuccess();

        page.Items.Select(i => i.Name).ShouldBe(["New", "Mid", "Old"]);
    }

    [Test]
    public async Task Search_SortByNameDescending()
    {
        GivenStored(Make("Alpha", "https://alpha.com"), Make("Beta", "https://beta.com"));

        var page = (await _service.SearchAsync(new CompanyQuery { Sort = "-NAME" }, CancellationToken.None)).ShouldBeSuccess();

        page.Items.Select(i => i.Name).ShouldBe(["Beta", "Alpha"]);
    }

    [TestCase(2, 2, new[] { "Co3", "Co4" })]
    [TestCase(3, 2, new[] { "Co5" })]
    [TestCase(4, 2, new string[0])]
    public async Task Search_Paging_SlicesAndReportsTotals(int page, int pageSize, string[] expected)
    {
        GivenStored(
            Make("Co1", "https://c1.com"), Make("Co2", "https://c2.com"), Make("Co3", "https://c3.com"),
            Make("Co4", "https://c4.com"), Make("Co5", "https://c5.com"));

        var result = (await _service.SearchAsync(new CompanyQuery { Page = page, PageSize = pageSize }, CancellationToken.None))
            .ShouldBeSuccess();

        result.ShouldSatisfyAllConditions(
            r => r.Items.Select(i => i.Name).ShouldBe(expected),
            r => r.Page.ShouldBe(page),
            r => r.TotalCount.ShouldBe(5),
            r => r.TotalPages.ShouldBe(3));
    }

    [TestCase(0, 20, null, null, "query.invalid_page", "page")]
    [TestCase(1, 0, null, null, "query.invalid_page_size", "pageSize")]
    [TestCase(1, 101, null, null, "query.invalid_page_size", "pageSize")]
    [TestCase(1, 20, "popularity", null, "query.invalid_sort", "sort")]
    [TestCase(1, 20, "relevance", null, "query.relevance_sort_requires_search", "sort")]
    public async Task Search_InvalidQuery_FailsValidation(
        int page, int pageSize, string? sort, string? search, string code, string target)
    {
        var query = new CompanyQuery { Page = page, PageSize = pageSize, Sort = sort, Search = search };

        var result = await _service.SearchAsync(query, CancellationToken.None);

        result.ShouldBeValidationError(code, target);
        await _repository.DidNotReceiveWithAnyArgs().ListAsync(default!, default);
    }

    [Test]
    public async Task Search_FilterTooLong_FailsValidation()
    {
        var query = new CompanyQuery { Search = new string('a', CompanyQuery.MaxFilterLength + 1) };

        var result = await _service.SearchAsync(query, CancellationToken.None);

        result.ShouldBeValidationError("query.filter_too_long", "q");
    }

    private void GivenStored(params Company[] companies) =>
        _repository.ListAsync(Arg.Any<CompanyFilter>(), Arg.Any<CancellationToken>()).Returns(companies);

    private static Company Make(string name, string website, DateTimeOffset? createdAt = null) =>
        Company.Restore(Guid.CreateVersion7(), CompanyName.Create(name).Value, WebsiteUrl.Create(website).Value, createdAt ?? Now);
}
