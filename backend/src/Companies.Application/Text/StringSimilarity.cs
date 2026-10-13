namespace Companies.Application.Text;

internal static class StringSimilarity
{
    private const double PrefixScale = 0.1;
    private const int MaxPrefixLength = 4;

    /// <summary>
    /// Jaro-Winkler similarity in [0, 1]: tolerant of typos and transpositions, and rewards a
    /// shared prefix. 1 means identical.
    /// </summary>
    public static double JaroWinkler(string first, string second)
    {
        ArgumentNullException.ThrowIfNull(first);
        ArgumentNullException.ThrowIfNull(second);

        var jaro = Jaro(first, second);

        var prefix = 0;
        var maxPrefix = Math.Min(MaxPrefixLength, Math.Min(first.Length, second.Length));
        while (prefix < maxPrefix && first[prefix] == second[prefix])
        {
            prefix++;
        }

        return jaro + (prefix * PrefixScale * (1 - jaro));
    }

    private static double Jaro(string first, string second)
    {
        if (first.Length == 0 && second.Length == 0)
        {
            return 1;
        }

        if (first.Length == 0 || second.Length == 0)
        {
            return 0;
        }

        var window = Math.Max(0, (Math.Max(first.Length, second.Length) / 2) - 1);
        var firstMatched = new bool[first.Length];
        var secondMatched = new bool[second.Length];
        var matches = 0;

        for (var i = 0; i < first.Length; i++)
        {
            var start = Math.Max(0, i - window);
            var end = Math.Min(i + window + 1, second.Length);

            for (var j = start; j < end; j++)
            {
                if (secondMatched[j] || first[i] != second[j])
                {
                    continue;
                }

                firstMatched[i] = true;
                secondMatched[j] = true;
                matches++;
                break;
            }
        }

        if (matches == 0)
        {
            return 0;
        }

        var outOfOrder = 0;
        var k = 0;
        for (var i = 0; i < first.Length; i++)
        {
            if (!firstMatched[i])
            {
                continue;
            }

            while (!secondMatched[k])
            {
                k++;
            }

            if (first[i] != second[k])
            {
                outOfOrder++;
            }

            k++;
        }

        double m = matches;
        var transpositions = outOfOrder / 2.0;

        return ((m / first.Length) + (m / second.Length) + ((m - transpositions) / m)) / 3;
    }
}
