using Companies.Application.Text;

namespace Companies.UnitTests.Application.Text;

[TestFixture]
public sealed class TextNormalizerTests
{
    [TestCase("Verus LLC", "verus llc")]
    [TestCase("  Blue   Bottle\tCoffee ", "blue bottle coffee")]
    [TestCase("Müller & Söhne", "muller sohne")]
    [TestCase("McDonald's", "mcdonalds")]
    [TestCase("McDonald’s", "mcdonalds")]
    [TestCase("S.A.", "sa")]
    [TestCase("U.S. Steel", "us steel")]
    [TestCase("Booking.com", "booking com")]
    [TestCase("AT&T", "at t")]
    [TestCase("Coca-Cola", "coca cola")]
    [TestCase("!!!", "")]
    public void Fold_LowercasesStripsDiacriticsAndPunctuation(string input, string expected)
    {
        TextNormalizer.Fold(input).ShouldBe(expected);
    }

    [Test]
    public void Words_SplitsFoldedText()
    {
        TextNormalizer.Words("The Blue-Bottle Co.").ShouldBe(["the", "blue", "bottle", "co"]);
    }

    [TestCase("müller", "muller")]
    [TestCase("blue-bottle", "bluebottle")]
    [TestCase("ACME", "acme")]
    public void FoldCompact_RemovesSeparators(string input, string expected)
    {
        TextNormalizer.FoldCompact(input).ShouldBe(expected);
    }
}
