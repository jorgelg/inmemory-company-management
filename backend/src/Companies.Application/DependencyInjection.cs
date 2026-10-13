using Companies.Application.Companies;
using Companies.Application.Relevance;
using Companies.Application.Search;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace Companies.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the company use cases and relevance checking. Requires an
    /// <see cref="IWebsiteContentFetcher"/> and an <see cref="ICompanyRepository"/> (Infrastructure).
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<RelevanceOptions>()
            .BindConfiguration(RelevanceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        // Registration order is evaluation order: the cheap offline check runs first, and the
        // validator stops at the first strategy that reaches the threshold.
        services.AddScoped<IRelevanceStrategy, DomainMatchStrategy>();
        services.AddScoped<IRelevanceStrategy, ContentInspectionStrategy>();
        services.AddScoped<IRelevanceValidator, RelevanceValidator>();

        services.AddSingleton<ICompanySearchRanker, CompanySearchRanker>();
        services.AddScoped<ICompanyService, CompanyService>();

        return services;
    }
}
