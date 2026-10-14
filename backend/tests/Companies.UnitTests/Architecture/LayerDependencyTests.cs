using System.Reflection;
using Companies.Domain.Companies;

namespace Companies.UnitTests.Architecture;

/// <summary>Guards the Clean Architecture dependency rule and the encapsulation of storage types.</summary>
[TestFixture]
public sealed class LayerDependencyTests
{
    private static readonly Assembly Domain = typeof(Company).Assembly;
    private static readonly Assembly Application = typeof(Companies.Application.DependencyInjection).Assembly;
    private static readonly Assembly Infrastructure = typeof(Companies.Infrastructure.DependencyInjection).Assembly;

    [Test]
    public void Domain_DependsOnNoOtherLayer()
    {
        SolutionReferences(Domain).ShouldBeEmpty();
    }

    [Test]
    public void Application_DependsOnlyOnDomain()
    {
        SolutionReferences(Application).ShouldBe(["Companies.Domain"]);
    }

    [Test]
    public void Infrastructure_DoesNotDependOnApi()
    {
        SolutionReferences(Infrastructure).ShouldNotContain("Companies.Api");
    }

    [Test]
    public void Infrastructure_ExposesOnlyItsRegistrationEntryPoint()
    {
        Infrastructure.GetExportedTypes().Select(t => t.FullName).ShouldBe(["Companies.Infrastructure.DependencyInjection"]);
    }

    private static string[] SolutionReferences(Assembly assembly) =>
    [
        .. assembly.GetReferencedAssemblies()
            .Select(reference => reference.Name!)
            .Where(name => name.StartsWith("Companies.", StringComparison.Ordinal))
            .Order(StringComparer.Ordinal),
    ];
}
