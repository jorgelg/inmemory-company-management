using System.Net;
using Microsoft.AspNetCore.Mvc.Testing;

namespace Companies.Api.IntegrationTests;

[TestFixture]
public sealed class ApiSmokeTests
{
    private WebApplicationFactory<Program> _factory = null!;

    [OneTimeSetUp]
    public void OneTimeSetUp() => _factory = new WebApplicationFactory<Program>();

    [OneTimeTearDown]
    public async Task OneTimeTearDown() => await _factory.DisposeAsync();

    [Test]
    public async Task OpenApiDocument_IsServed_InDevelopment()
    {
        using var client = _factory.CreateClient();

        using var response = await client.GetAsync(new Uri("/openapi/v1.json", UriKind.Relative));

        Assert.That(response.StatusCode, Is.EqualTo(HttpStatusCode.OK));
    }
}
