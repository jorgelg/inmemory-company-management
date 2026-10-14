using Companies.Application.Relevance;
using Companies.Domain.Companies;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;

namespace Companies.Api.IntegrationTests.Support;

/// <summary>
/// Hosts the real API in memory with a fresh in-memory store. The website content fetcher is
/// substituted so tests never touch the network; by default it finds no content. DI validation is
/// on, so every test also proves the composition root is complete.
/// </summary>
public sealed class CompaniesApiFactory : WebApplicationFactory<Program>
{
    private readonly Action<IServiceCollection>? _configureServices;

    public CompaniesApiFactory(Action<IServiceCollection>? configureServices = null)
    {
        _configureServices = configureServices;
        ContentFetcher.FetchAsync(Arg.Any<WebsiteUrl>(), Arg.Any<CancellationToken>()).Returns((WebsiteContent?)null);
    }

    public IWebsiteContentFetcher ContentFetcher { get; } = Substitute.For<IWebsiteContentFetcher>();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.UseDefaultServiceProvider(options =>
        {
            options.ValidateOnBuild = true;
            options.ValidateScopes = true;
        });
        builder.ConfigureTestServices(services =>
        {
            services.AddScoped(_ => ContentFetcher);
            _configureServices?.Invoke(services);
        });
    }
}
