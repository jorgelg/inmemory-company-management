using Companies.Domain.Common;
using Companies.Domain.Companies;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class CompanyNameTests
{
    [TestCase("Verus LLC", "Verus LLC")]
    [TestCase("  Acme  ", "Acme")]
    [TestCase("Blue   Bottle\tCoffee", "Blue Bottle Coffee")]
    [TestCase("IBM", "IBM")]
    public void Create_WithValidName_NormalizesWhitespace(string input, string expected)
    {
        var result = CompanyName.Create(input);

        Assert.That(result.IsSuccess, Is.True);
        Assert.That(result.Value.Value, Is.EqualTo(expected));
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithMissingName_FailsAsRequired(string? input)
    {
        AssertValidationError(CompanyName.Create(input), "company_name.required");
    }

    [TestCase("A")]
    [TestCase(" AB ")]
    public void Create_WithTooShortName_Fails(string input)
    {
        AssertValidationError(CompanyName.Create(input), "company_name.too_short");
    }

    [Test]
    public void Create_WithTooLongName_Fails()
    {
        var input = new string('a', CompanyName.MaxLength + 1);

        AssertValidationError(CompanyName.Create(input), "company_name.too_long");
    }

    [Test]
    public void Create_AtMaxLength_Succeeds()
    {
        var result = CompanyName.Create(new string('a', CompanyName.MaxLength));

        Assert.That(result.IsSuccess, Is.True);
    }

    [TestCase("!!!")]
    [TestCase("- - -")]
    public void Create_WithoutLettersOrDigits_Fails(string input)
    {
        AssertValidationError(CompanyName.Create(input), "company_name.no_alphanumeric");
    }

    [Test]
    public void Equality_IsByValue()
    {
        Assert.That(CompanyName.Create("Acme").Value, Is.EqualTo(CompanyName.Create(" Acme ").Value));
    }

    private static void AssertValidationError(Result<CompanyName> result, string code)
    {
        Assert.That(result.IsSuccess, Is.False);
        Assert.Multiple(() =>
        {
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Validation));
            Assert.That(result.Error.Code, Is.EqualTo(code));
            Assert.That(result.Error.Target, Is.EqualTo("name"));
            Assert.That(result.Error.Message, Is.Not.Empty);
        });
    }
}
