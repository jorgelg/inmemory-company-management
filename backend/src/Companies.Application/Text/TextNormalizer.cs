using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace Companies.Application.Text;

/// <summary>
/// Folds free text into a comparable form: lower-case, diacritics removed, apostrophes dropped,
/// dotted initialisms joined ("S.A." → "sa"), every other non-alphanumeric run turned into one space.
/// </summary>
internal static partial class TextNormalizer
{
    public static string Fold(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return string.Empty;
        }

        var decomposed = text.ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);

        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) == UnicodeCategory.NonSpacingMark
                || character is '\'' or '’' or '‘' or '`')
            {
                continue;
            }

            builder.Append(character);
        }

        var withoutMarks = builder.ToString().Normalize(NormalizationForm.FormC);
        var joinedInitialisms = DottedInitialism().Replace(
            withoutMarks, match => match.Value.Replace(".", string.Empty, StringComparison.Ordinal));

        return NonAlphanumericRun().Replace(joinedInitialisms, " ").Trim();
    }

    public static string[] Words(string? text) =>
        Fold(text).Split(' ', StringSplitOptions.RemoveEmptyEntries);

    public static string FoldCompact(string? text) =>
        Fold(text).Replace(" ", string.Empty, StringComparison.Ordinal);

    [GeneratedRegex(@"(?<![\p{L}\p{N}])(?:\p{L}\.){2,}")]
    private static partial Regex DottedInitialism();

    [GeneratedRegex(@"[^\p{L}\p{N}]+")]
    private static partial Regex NonAlphanumericRun();
}
