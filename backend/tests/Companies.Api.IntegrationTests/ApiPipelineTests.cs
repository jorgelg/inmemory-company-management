using System.Net;
using Companies.Api.IntegrationTests.Support;
using Companies.Application.Companies;
using Companies.Application.Companies.Contracts;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Companies.Api.IntegrationTests;

/// <summary>Cross-cutting behaviour: OpenAPI, unknown routes, unhandled exceptions, CORS.</summary>
[TestFixture]
public sealed class ApiPipelineTests
{
    [Test]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        var document = await response.Content.ReadAsStringAsync();
        document.ShouldSatisfyAllConditions(
            d => d.ShouldContain("/api/companies"),
            d => d.ShouldContain("\"title\": \"Companies API\""));
    }

    [Test]
    public async Task ScalarApiReference_IsServed_InDevelopment()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/scalar/v1", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("text/html");
        // The page loads the document relative to the site root.
        (await response.Content.ReadAsStringAsync()).ShouldContain("\"url\":\"openapi/v1.json\"");
    }

    [TestCase("/scalar/v1")]
    [TestCase("/openapi/v1.json")]
    public async Task ApiDocumentation_IsNotServed_OutsideDevelopment(string path)
    {
        await using var factory = new CompaniesApiFactory();
        await using var production = factory.WithWebHostBuilder(builder => builder.UseEnvironment("Production"));
        using var client = production.CreateClient();

        using var response = await client.GetAsync(new Uri(path, UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    [Test]
    public async Task UnknownRoute_Returns404Problem()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/nothing-here", UriKind.Relative));

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/problem+json");
    }

    [Test]
    public async Task UnhandledException_Returns500ProblemWithoutDetails()
    {
        var service = Substitute.For<ICompanyService>();
        service.SearchAsync(Arg.Any<CompanyQuery>(), Arg.Any<CancellationToken>())
            .Returns<Task<Domain.Common.Result<PagedResponse<CompanyResponse>>>>(_ => throw new InvalidOperationException("secret internals"));
        await using var factory = new CompaniesApiFactory(services => services.AddScoped(_ => service));
        using var client = factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/api/companies", UriKind.Relative));

        await response.ShouldBeProblem(HttpStatusCode.InternalServerError);
        (await response.Content.ReadAsStringAsync()).ShouldNotContain("secret internals");
    }

    [Test]
    public async Task CorsPreflight_FromFrontendOrigin_IsAllowed()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/companies");
        request.Headers.Add("Origin", "http://localhost:4200");
        request.Headers.Add("Access-Control-Request-Method", "POST");
        request.Headers.Add("Access-Control-Request-Headers", "content-type");

        using var response = await client.SendAsync(request);

        response.StatusCode.ShouldBe(HttpStatusCode.NoContent);
        response.Headers.GetValues("Access-Control-Allow-Origin").ShouldBe(["http://localhost:4200"]);
    }

    [Test]
    public async Task Cors_ExposesLocationHeader()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Get, "/api/companies");
        request.Headers.Add("Origin", "http://localhost:4200");

        using var response = await client.SendAsync(request);

        response.Headers.GetValues("Access-Control-Expose-Headers").ShouldContain(h => h.Contains("Location", StringComparison.OrdinalIgnoreCase));
    }

    [Test]
    public async Task CorsPreflight_FromUnknownOrigin_IsNotAllowed()
    {
        await using var factory = new CompaniesApiFactory();
        using var client = factory.CreateClient();
        using var request = new HttpRequestMessage(HttpMethod.Options, "/api/companies");
        request.Headers.Add("Origin", "https://evil.example");
        request.Headers.Add("Access-Control-Request-Method", "POST");

        using var response = await client.SendAsync(request);

        response.Headers.Contains("Access-Control-Allow-Origin").ShouldBeFalse();
    }
}
