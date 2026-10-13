using Companies.Domain.Common;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class ResultTests
{
    [Test]
    public void Success_ExposesValue()
    {
        Result<int> result = 42;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.True);
            Assert.That(result.Value, Is.EqualTo(42));
        });
    }

    [Test]
    public void Failure_ExposesError()
    {
        var error = Error.Validation("company_name.required", "Company name is required.", "name");

        Result<int> result = error;

        Assert.Multiple(() =>
        {
            Assert.That(result.IsSuccess, Is.False);
            Assert.That(result.Error, Is.EqualTo(error));
            Assert.That(result.Error.Type, Is.EqualTo(ErrorType.Validation));
        });
    }

    [Test]
    public void Value_OnFailure_Throws()
    {
        Result<int> result = Error.NotFound("company.not_found", "Company was not found.");

        Assert.That(() => result.Value, Throws.InvalidOperationException);
    }

    [Test]
    public void Error_OnSuccess_Throws()
    {
        Result<int> result = 1;

        Assert.That(() => result.Error, Throws.InvalidOperationException);
    }
}
