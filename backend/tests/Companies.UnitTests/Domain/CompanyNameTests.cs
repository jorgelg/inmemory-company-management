using Companies.Domain.Companies;
using Companies.UnitTests.Support;

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
        CompanyName.Create(input).ShouldBeSuccess().Value.ShouldBe(expected);
    }

    [TestCase(null)]
    [TestCase("")]
    [TestCase("   ")]
    public void Create_WithMissingName_FailsAsRequired(string? input)
    {
        CompanyName.Create(input).ShouldBeValidationError("company_name.required", "name");
    }

    [TestCase("A")]
    [TestCase(" AB ")]
    public void Create_WithTooShortName_Fails(string input)
    {
        CompanyName.Create(input).ShouldBeValidationError("company_name.too_short", "name");
    }

    [Test]
    public void Create_WithTooLongName_Fails()
    {
        var input = new string('a', CompanyName.MaxLength + 1);

        CompanyName.Create(input).ShouldBeValidationError("company_name.too_long", "name");
    }

    [Test]
    public void Create_AtMaxLength_Succeeds()
    {
        CompanyName.Create(new string('a', CompanyName.MaxLength)).ShouldBeSuccess();
    }

    [TestCase("!!!")]
    [TestCase("- - -")]
    public void Create_WithoutLettersOrDigits_Fails(string input)
    {
        CompanyName.Create(input).ShouldBeValidationError("company_name.no_alphanumeric", "name");
    }

    [Test]
    public void Equality_IsByValue()
    {
        CompanyName.Create("Acme").Value.ShouldBe(CompanyName.Create(" Acme ").Value);
    }
}
