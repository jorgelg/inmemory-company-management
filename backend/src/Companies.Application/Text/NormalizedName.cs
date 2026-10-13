namespace Companies.Application.Text;

/// <summary>A company name reduced to the words that identify it.</summary>
/// <param name="Words">Folded words with trailing legal suffixes removed ("Bank of America Corp" → bank, of, america).</param>
/// <param name="SignificantWords">Words without generic stop words (bank, america).</param>
/// <param name="Acronyms">Initials of <paramref name="Words"/> and <paramref name="SignificantWords"/> for multi-word names (boa, ba).</param>
public sealed record NormalizedName(
    IReadOnlyList<string> Words,
    IReadOnlyList<string> SignificantWords,
    IReadOnlyList<string> Acronyms)
{
    /// <summary>Significant words joined, e.g. <c>bankamerica</c>.</summary>
    public string Compact { get; } = string.Concat(SignificantWords);

    /// <summary>All words joined, e.g. <c>bankofamerica</c>.</summary>
    public string FullCompact { get; } = string.Concat(Words);
}
