using Companies.Application.Companies;
using Companies.Application.Relevance;
using Companies.Infrastructure.Persistence;
using Companies.Infrastructure.WebContent;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Companies.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the in-memory company store and the SSRF-guarded website content fetcher.
    /// To move to a database, register a different <see cref="ICompanyRepository"/> here; nothing
    /// else changes.
    /// </summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddSingleton<ICompanyRepository, InMemoryCompanyRepository>();

        services.AddOptions<WebsiteFetcherOptions>()
            .BindConfiguration(WebsiteFetcherOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddHttpClient<IWebsiteContentFetcher, HttpWebsiteContentFetcher>((provider, client) =>
            {
                var options = provider.GetRequiredService<IOptions<WebsiteFetcherOptions>>().Value;

                // Backstop only: the fetcher enforces options.Timeout across all redirect hops itself.
                client.Timeout = options.Timeout + TimeSpan.FromSeconds(1);
                client.DefaultRequestHeaders.UserAgent.ParseAdd(options.UserAgent);
            })
            .ConfigurePrimaryHttpMessageHandler(provider =>
                GuardedHttpHandler.Create(provider.GetRequiredService<IOptions<WebsiteFetcherOptions>>().Value));

        return services;
    }
}
