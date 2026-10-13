using Companies.Domain.Common;

namespace Companies.Domain.Companies;

/// <summary>Validation errors raised while building company value objects.</summary>
public static class CompanyErrors
{
    public const string NameField = "name";
    public const string WebsiteField = "website";

    public static readonly Error NameRequired = Error.Validation(
        "company_name.required", "Company name is required.", NameField);

    public static readonly Error NameTooShort = Error.Validation(
        "company_name.too_short", $"Company name must be at least {CompanyName.MinLength} characters.", NameField);

    public static readonly Error NameTooLong = Error.Validation(
        "company_name.too_long", $"Company name must be at most {CompanyName.MaxLength} characters.", NameField);

    public static readonly Error NameNoAlphanumeric = Error.Validation(
        "company_name.no_alphanumeric", "Company name must contain letters or digits.", NameField);

    public static readonly Error WebsiteRequired = Error.Validation(
        "website.required", "Website URL is required.", WebsiteField);

    public static readonly Error WebsiteTooLong = Error.Validation(
        "website.too_long", $"Website URL must be at most {WebsiteUrl.MaxLength} characters.", WebsiteField);

    public static readonly Error WebsiteMalformed = Error.Validation(
        "website.malformed", "Website URL must be a well-formed absolute URL, e.g. https://www.example.com.", WebsiteField);

    public static readonly Error WebsiteUnsupportedScheme = Error.Validation(
        "website.unsupported_scheme", "Website URL must use http or https.", WebsiteField);

    public static readonly Error WebsiteCredentialsNotAllowed = Error.Validation(
        "website.credentials_not_allowed", "Website URL must not contain a user name or password.", WebsiteField);

    public static readonly Error WebsiteInvalidHost = Error.Validation(
        "website.invalid_host", "Website URL must use a public domain name, e.g. example.com.", WebsiteField);
}
