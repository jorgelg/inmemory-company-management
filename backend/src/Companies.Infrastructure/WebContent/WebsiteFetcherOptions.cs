using System.ComponentModel.DataAnnotations;

namespace Companies.Infrastructure.WebContent;

/// <summary>Configuration section <c>WebsiteFetcher</c>: limits for reading a company's home page.</summary>
internal sealed class WebsiteFetcherOptions
{
    public const string SectionName = "WebsiteFetcher";

    /// <summary>Total time budget for one fetch, redirects included.</summary>
    [Range(typeof(TimeSpan), "00:00:00.050", "00:00:30")]
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(3);

    /// <summary>Only this many bytes of the page are read; the title and headings sit near the top.</summary>
    [Range(1024, 4 * 1024 * 1024)]
    public int MaxResponseBytes { get; set; } = 256 * 1024;

    [Range(0, 10)]
    public int MaxRedirects { get; set; } = 3;

    [Required]
    public string UserAgent { get; set; } = "CompaniesApi/1.0 (company relevance check)";
}
