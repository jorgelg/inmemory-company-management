using System.Net;
using System.Net.Http.Json;
using Companies.Api.IntegrationTests.Support;
using Companies.Application.Companies.Contracts;

namespace Companies.Api.IntegrationTests;

[TestFixture]
public sealed class QueryCompaniesTests
{
    private CompaniesApiFactory _factory = null!;
    private HttpClient _client = null!;

    [SetUp]
    public void SetUp()
    {
        _factory = new CompaniesApiFactory();
        _client = _factory.CreateClient();
    }

    [TearDown]
    public async Task TearDown()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
    }

    [Test]
    public async Task GetById_Unknown_Returns404Problem()
    {
        var id = Guid.CreateVersion7();

        using var response = await Get($"/api/companies/{id}");

        var problem = await response.ShouldBeProblem(HttpStatusCode.NotFound, "company.not_found");
        problem.GetProperty("detail").GetString().ShouldNotBeNull().ShouldContain(id.ToString());
    }

    [Test]
    public async Task GetById_NotAGuid_Returns404Problem()
    {
        using var response = await Get("/api/companies/not-a-guid");

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task List_EmptyStore_Returns200WithEmptyPage()
    {
        using var response = await Get("/api/companies");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.ShouldSatisfyAllConditions(
            p => p.Items.ShouldBeEmpty(),
            p => p.TotalCount.ShouldBe(0),
            p => p.Page.ShouldBe(1),
            p => p.PageSize.ShouldBe(20));
    }

    [Test]
    public async Task List_ReturnsAllSortedByNameWithTotals()
    {
        await Seed();

        using var response = await Get("/api/companies");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.ShouldSatisfyAllConditions(
            p => p.Items.Select(i => i.Name).ShouldBe(["Acme Rockets", "Blue Bottle Coffee", "Verus Capital", "Verus LLC"]),
            p => p.TotalCount.ShouldBe(4),
            p => p.Items.ShouldAllBe(i => i.RelevanceScore == null));
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"totalPages\":1");
    }

    [TestCase("name=verus", new[] { "Verus Capital", "Verus LLC" })]
    [TestCase("name=Blue%20Bottle%20Coffee", new[] { "Blue Bottle Coffee" })]
    [TestCase("domain=verus.com", new[] { "Verus LLC" })]
    [TestCase("domain=https%3A%2F%2Fwww.acmerockets.com%2Fabout", new[] { "Acme Rockets" })]
    [TestCase("domain=verus", new[] { "Verus Capital", "Verus LLC" })]
    [TestCase("name=verus&domain=veruscapital.com", new[] { "Verus Capital" })]
    [TestCase("name=nothing-matches", new string[0])]
    public async Task List_Filters(string query, string[] expected)
    {
        await Seed();

        using var response = await Get($"/api/companies?{query}");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.Items.Select(i => i.Name).ShouldBe(expected);
    }

    [Test]
    public async Task List_Search_OrdersByRelevanceWithScores()
    {
        await Seed();

        using var response = await Get("/api/companies?q=verus");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.Items.Select(i => (i.Name, i.RelevanceScore)).ShouldBe([("Verus LLC", 1.0), ("Verus Capital", 0.8)]);
    }

    [Test]
    public async Task List_SortByCreatedAtDescending_ReturnsNewestFirst()
    {
        await Seed();

        using var response = await Get("/api/companies?sort=-createdAt");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.Items.Select(i => i.Name).ShouldBe(["Verus Capital", "Blue Bottle Coffee", "Acme Rockets", "Verus LLC"]);
    }

    [Test]
    public async Task List_Paging_ReturnsRequestedSlice()
    {
        await Seed();

        using var response = await Get("/api/companies?page=2&pageSize=3");

        var page = await response.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.ShouldSatisfyAllConditions(
            p => p.Items.Select(i => i.Name).ShouldBe(["Verus LLC"]),
            p => p.Page.ShouldBe(2),
            p => p.PageSize.ShouldBe(3),
            p => p.TotalCount.ShouldBe(4),
            p => p.TotalPages.ShouldBe(2));
    }

    [TestCase("pageSize=0", "query.invalid_page_size", "pageSize")]
    [TestCase("pageSize=101", "query.invalid_page_size", "pageSize")]
    [TestCase("page=0", "query.invalid_page", "page")]
    [TestCase("sort=popularity", "query.invalid_sort", "sort")]
    [TestCase("sort=relevance", "query.relevance_sort_requires_search", "sort")]
    public async Task List_InvalidQuery_Returns400WithCode(string query, string code, string field)
    {
        using var response = await Get($"/api/companies?{query}");

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest, code);
        problem.FieldErrors(field).ShouldNotBeEmpty();
    }

    [Test]
    public async Task List_NonNumericPage_Returns400()
    {
        using var response = await Get("/api/companies?page=abc");

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest);
        problem.FieldErrors("page").ShouldNotBeEmpty();
    }

    private Task<HttpResponseMessage> Get(string path) => _client.GetAsync(new Uri(path, UriKind.Relative));

    /// <summary>Adds four companies in this order, so creation order differs from name order.</summary>
    private async Task Seed()
    {
        foreach (var (name, website) in new[]
                 {
                     ("Verus LLC", "https://www.verus.com"),
                     ("Acme Rockets", "https://www.acmerockets.com"),
                     ("Blue Bottle Coffee", "https://bluebottlecoffee.com"),
                     ("Verus Capital", "https://veruscapital.com"),
                 })
        {
            using var response = await _client.PostAsJsonAsync(
                new Uri("/api/companies", UriKind.Relative), new { name, website }, HttpAssertions.Json);
            response.StatusCode.ShouldBe(HttpStatusCode.Created);
            await Task.Delay(2); // distinct CreatedAt timestamps
        }
    }
}
