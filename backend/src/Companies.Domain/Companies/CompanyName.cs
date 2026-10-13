using System.Text.RegularExpressions;
using Companies.Domain.Common;

namespace Companies.Domain.Companies;

/// <summary>A company's display name: trimmed, inner whitespace collapsed, 3–200 characters.</summary>
public sealed partial record CompanyName
{
    public const int MinLength = 3;
    public const int MaxLength = 200;

    private CompanyName(string value) => Value = value;

    public string Value { get; }

    public static Result<CompanyName> Create(string? input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return CompanyErrors.NameRequired;
        }

        var normalized = WhitespaceRun().Replace(input.Trim(), " ");

        if (normalized.Length < MinLength)
        {
            return CompanyErrors.NameTooShort;
        }

        if (normalized.Length > MaxLength)
        {
            return CompanyErrors.NameTooLong;
        }

        if (!normalized.Any(char.IsLetterOrDigit))
        {
            return CompanyErrors.NameNoAlphanumeric;
        }

        return new CompanyName(normalized);
    }

    public override string ToString() => Value;

    [GeneratedRegex(@"\s+")]
    private static partial Regex WhitespaceRun();
}
