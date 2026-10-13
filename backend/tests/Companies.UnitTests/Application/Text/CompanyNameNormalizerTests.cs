using Companies.Application.Text;
using Companies.Domain.Companies;

namespace Companies.UnitTests.Application.Text;

[TestFixture]
public sealed class CompanyNameNormalizerTests
{
    [TestCase("Verus LLC", new[] { "verus" }, "verus")]
    [TestCase("Acme, Inc.", new[] { "acme" }, "acme")]
    [TestCase("The Blue Bottle Coffee Company", new[] { "blue", "bottle", "coffee" }, "bluebottlecoffee")]
    [TestCase("Müller GmbH", new[] { "muller" }, "muller")]
    [TestCase("Siemens AG", new[] { "siemens" }, "siemens")]
    [TestCase("Nestlé S.A.", new[] { "nestle" }, "nestle")]
    [TestCase("Acme Holdings Group Ltd", new[] { "acme" }, "acme")]
    [TestCase("Bank of America Corporation", new[] { "bank", "america" }, "bankamerica")]
    [TestCase("AT&T Inc.", new[] { "at", "t" }, "att")]
    public void Normalize_RemovesLegalSuffixesAndStopWords(string input, string[] significant, string compact)
    {
        var normalized = CompanyNameNormalizer.Normalize(Name(input));

        normalized.ShouldSatisfyAllConditions(
            n => n.SignificantWords.ShouldBe(significant),
            n => n.Compact.ShouldBe(compact));
    }

    [Test]
    public void Normalize_KeepsStopWordsInWords()
    {
        var normalized = CompanyNameNormalizer.Normalize(Name("Bank of America Corp"));

        normalized.Words.ShouldBe(["bank", "of", "america"]);
    }

    [Test]
    public void Normalize_WhenOnlyGenericWords_FallsBackToAllWords()
    {
        var normalized = CompanyNameNormalizer.Normalize(Name("The Company LLC"));

        normalized.SignificantWords.ShouldBe(["the", "company", "llc"]);
    }

    [TestCase("International Business Machines", "ibm")]
    [TestCase("Bank of America", "boa")]
    [TestCase("Bank of America", "ba")]
    public void Acronyms_IncludeSignificantAndFullForms(string input, string acronym)
    {
        CompanyNameNormalizer.Normalize(Name(input)).Acronyms.ShouldContain(acronym);
    }

    [Test]
    public void Acronyms_IgnoreSingleWordNames()
    {
        CompanyNameNormalizer.Normalize(Name("Acme")).Acronyms.ShouldBeEmpty();
    }

    private static CompanyName Name(string value) => CompanyName.Create(value).Value;
}
