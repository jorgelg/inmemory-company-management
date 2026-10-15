using System.Net;
using System.Net.Http.Json;
using System.Text;
using Companies.Api.IntegrationTests.Support;
using Companies.Application.Companies.Contracts;
using Companies.Application.Relevance;
using Companies.Domain.Companies;
using NSubstitute;

namespace Companies.Api.IntegrationTests;

[TestFixture]
public sealed class CreateCompanyTests
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
    public async Task Post_ValidCompany_Returns201WithLocationAndBody()
    {
        using var response = await Post(new { name = "Verus LLC", website = "https://www.verus.com/" });

        var company = await response.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.Created);
        company.ShouldSatisfyAllConditions(
            c => c.Id.ShouldNotBe(Guid.Empty),
            c => c.Name.ShouldBe("Verus LLC"),
            c => c.Website.ShouldBe("https://www.verus.com"),
            c => c.Domain.ShouldBe("verus.com"));
        response.Headers.Location.ShouldNotBeNull().AbsolutePath.ShouldBe($"/api/companies/{company.Id}");
    }

    [Test]
    public async Task Post_ThenGetLocation_ReturnsSameCompany()
    {
        using var created = await Post(new { name = "Verus LLC", website = "https://www.verus.com" });
        var company = await created.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.Created);

        using var fetched = await _client.GetAsync(created.Headers.Location);

        (await fetched.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.OK)).ShouldBe(company);
    }

    [Test]
    public async Task Post_ValidCompany_DoesNotIncludeRelevanceScore()
    {
        using var response = await Post(new { name = "Verus LLC", website = "https://www.verus.com" });

        (await response.Content.ReadAsStringAsync()).ShouldNotContain("relevanceScore");
    }

    [TestCase(null, "name")]
    [TestCase("", "name")]
    [TestCase("A", "name")]
    public async Task Post_InvalidName_Returns400WithFieldError(string? name, string field)
    {
        using var response = await Post(new { name, website = "https://www.verus.com" });

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest);
        problem.FieldErrors(field).ShouldNotBeEmpty();
    }

    [TestCase(null)]
    [TestCase("not a url")]
    [TestCase("www.verus.com")]
    public async Task Post_InvalidWebsite_Returns400WithFieldError(string? website)
    {
        using var response = await Post(new { name = "Verus LLC", website });

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest);
        problem.FieldErrors("website").ShouldNotBeEmpty();
    }

    [Test]
    public async Task Post_BothFieldsInvalid_ReportsBothFields()
    {
        using var response = await Post(new { name = "", website = "nope" });

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest);
        problem.ShouldSatisfyAllConditions(
            p => p.FieldErrors("name").ShouldNotBeEmpty(),
            p => p.FieldErrors("website").ShouldNotBeEmpty());
    }

    [TestCase("  AB  ", "https://www.verus.com", "company_name.too_short", "name")]
    [TestCase("Verus LLC", "https://localhost", "website.invalid_host", "website")]
    [TestCase("Verus LLC", "ftp://verus.com", "website.unsupported_scheme", "website")]
    public async Task Post_FailsDomainValidation_Returns400WithCode(string name, string website, string code, string field)
    {
        using var response = await Post(new { name, website });

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest, code);
        problem.FieldErrors(field).ShouldHaveSingleItem().ShouldBe(problem.GetProperty("detail").GetString());
    }

    [Test]
    public async Task Post_NameNotRelevantToWebsite_Returns400WithExplanation()
    {
        using var response = await Post(new { name = "Acme", website = "https://www.example.com" });

        var problem = await response.ShouldBeProblem(HttpStatusCode.BadRequest, "company.not_relevant");
        problem.GetProperty("detail").GetString().ShouldNotBeNull().ShouldSatisfyAllConditions(
            d => d.ShouldContain("'Acme'"),
            d => d.ShouldContain("example.com"),
            d => d.ShouldContain("threshold 0.60"));
    }

    [Test]
    public async Task Post_BrandWithUnrelatedDomain_IsAcceptedViaContentInspection()
    {
        _factory.ContentFetcher.FetchAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>())
            .Returns(new WebsiteContent(Title: "Alphabet", SiteName: null, Heading: null));

        using var response = await Post(new { name = "Alphabet Inc.", website = "https://abc.xyz" });

        (await response.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.Created)).Domain.ShouldBe("abc.xyz");
    }

    [Test]
    public async Task Post_DomainMatch_DoesNotFetchWebsite()
    {
        using var response = await Post(new { name = "Verus LLC", website = "https://www.verus.com" });

        response.StatusCode.ShouldBe(HttpStatusCode.Created);
        await _factory.ContentFetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);
    }

    [Test]
    public async Task Post_DuplicateDomain_Returns409()
    {
        using var first = await Post(new { name = "Verus LLC", website = "https://www.verus.com" });
        first.StatusCode.ShouldBe(HttpStatusCode.Created);

        using var second = await Post(new { name = "Verus", website = "https://shop.verus.com" });

        var problem = await second.ShouldBeProblem(HttpStatusCode.Conflict, "company.duplicate_website");
        problem.FieldErrors("website").ShouldNotBeEmpty();
    }

    [Test]
    public async Task Post_DifferentCompaniesUnderSameCountrySecondLevel_AreNotDuplicates()
    {
        using var google = await Post(new { name = "Google", website = "https://www.google.com.bo" });
        using var tigo = await Post(new { name = "Tigo", website = "https://www.tigo.com.bo" });

        (await google.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.Created)).Domain.ShouldBe("google.com.bo");
        (await tigo.ShouldHaveJsonBody<CompanyResponse>(HttpStatusCode.Created)).Domain.ShouldBe("tigo.com.bo");
        await _factory.ContentFetcher.DidNotReceiveWithAnyArgs().FetchAsync(default!, default);

        using var filtered = await _client.GetAsync(new Uri("/api/companies?domain=google.com.bo", UriKind.Relative));
        var page = await filtered.ShouldHaveJsonBody<PagedResponse<CompanyResponse>>(HttpStatusCode.OK);
        page.Items.Select(i => i.Name).ShouldBe(["Google"]);
    }

    [TestCase("{ not json")]
    [TestCase("")]
    [TestCase("[]")]
    public async Task Post_MalformedBody_Returns400(string body)
    {
        using var content = new StringContent(body, Encoding.UTF8, "application/json");

        using var response = await _client.PostAsync(new Uri("/api/companies", UriKind.Relative), content);

        await response.ShouldBeProblem(HttpStatusCode.BadRequest);
    }

    [Test]
    public async Task Post_WrongContentType_Returns415()
    {
        using var content = new StringContent("name=Verus", Encoding.UTF8, "text/plain");

        using var response = await _client.PostAsync(new Uri("/api/companies", UriKind.Relative), content);

        response.StatusCode.ShouldBe(HttpStatusCode.UnsupportedMediaType);
    }

    private Task<HttpResponseMessage> Post(object body) =>
        _client.PostAsJsonAsync(new Uri("/api/companies", UriKind.Relative), body, HttpAssertions.Json);
}
