using Companies.Application.Text;

namespace Companies.UnitTests.Application.Text;

[TestFixture]
public sealed class StringSimilarityTests
{
    // Reference values from Winkler (1990) / the usual Jaro-Winkler test pairs.
    [TestCase("martha", "marhta", 0.961)]
    [TestCase("dwayne", "duane", 0.840)]
    [TestCase("dixon", "dicksonx", 0.813)]
    [TestCase("gogle", "google", 0.956)]
    public void JaroWinkler_MatchesReferenceValues(string a, string b, double expected)
    {
        StringSimilarity.JaroWinkler(a, b).ShouldBe(expected, tolerance: 0.001);
    }

    [TestCase("acme", "acme", 1.0)]
    [TestCase("", "", 1.0)]
    [TestCase("abc", "", 0.0)]
    [TestCase("abc", "xyz", 0.0)]
    public void JaroWinkler_HandlesEdgeCases(string a, string b, double expected)
    {
        StringSimilarity.JaroWinkler(a, b).ShouldBe(expected, tolerance: 0.0001);
    }

    [Test]
    public void JaroWinkler_IsSymmetric()
    {
        StringSimilarity.JaroWinkler("verus", "virus")
            .ShouldBe(StringSimilarity.JaroWinkler("virus", "verus"), tolerance: 0.0001);
    }
}
