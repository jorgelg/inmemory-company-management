using System.ComponentModel.DataAnnotations;
using Companies.Domain.Companies;

namespace Companies.Application.Companies.Contracts;

/// <summary>
/// Payload for registering a company. The annotations give fast, field-level feedback at the
/// API edge; the domain value objects and the relevance check remain the source of truth.
/// </summary>
public sealed record CreateCompanyRequest
{
    [Required(ErrorMessage = "Company name is required.")]
    [StringLength(CompanyName.MaxLength, MinimumLength = CompanyName.MinLength,
        ErrorMessage = "Company name must be between {2} and {1} characters.")]
    public string? Name { get; init; }

    [Required(ErrorMessage = "Website URL is required.")]
    [StringLength(WebsiteUrl.MaxLength, ErrorMessage = "Website URL must be at most {1} characters.")]
    [Url(ErrorMessage = "Website URL must be a well-formed absolute URL, e.g. https://www.example.com.")]
    public string? Website { get; init; }
}
