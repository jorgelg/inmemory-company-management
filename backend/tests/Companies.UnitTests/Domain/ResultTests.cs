using Companies.Domain.Common;
using Companies.UnitTests.Support;

namespace Companies.UnitTests.Domain;

[TestFixture]
public sealed class ResultTests
{
    [Test]
    public void Success_ExposesValue()
    {
        Result<int> result = 42;

        result.ShouldSatisfyAllConditions(
            () => result.IsSuccess.ShouldBeTrue(),
            () => result.Value.ShouldBe(42));
    }

    [Test]
    public void Failure_ExposesError()
    {
        var error = Error.Validation("company_name.required", "Company name is required.", "name");

        Result<int> result = error;

        result.ShouldBeFailure().ShouldBe(error);
    }

    [Test]
    public void Value_OnFailure_Throws()
    {
        Result<int> result = Error.NotFound("company.not_found", "Company was not found.");

        Should.Throw<InvalidOperationException>(() => result.Value);
    }

    [Test]
    public void Error_OnSuccess_Throws()
    {
        Result<int> result = 1;

        Should.Throw<InvalidOperationException>(() => result.Error);
    }

    [Test]
    public void Match_InvokesBranchForOutcome()
    {
        Result<int> success = 2;
        Result<int> failure = Error.Conflict("company.duplicate", "Already exists.");

        success.Match(v => $"ok:{v}", e => e.Code).ShouldBe("ok:2");
        failure.Match(v => $"ok:{v}", e => e.Code).ShouldBe("company.duplicate");
    }
}
