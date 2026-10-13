using Companies.Domain.Companies;

namespace Companies.Application.Relevance;

/// <summary>
/// Port for reading a website's identifying text. Implementations must never throw for network
/// or content problems; they return <see langword="null"/> when the content is unavailable.
/// </summary>
public interface IWebsiteContentFetcher
{
    Task<WebsiteContent?> FetchAsync(WebsiteUrl website, CancellationToken cancellationToken);
}
