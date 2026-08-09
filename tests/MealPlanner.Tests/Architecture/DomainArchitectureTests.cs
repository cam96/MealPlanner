using System.Reflection;
using MealPlanner.Domain.Entities;
using NetArchTest.Rules;

namespace MealPlanner.Tests.Architecture;

/// <summary>
/// Ensures the Domain layer remains pure — no framework or I/O dependencies.
/// </summary>
[TestFixture]
public class DomainArchitectureTests
{
    private static readonly Assembly DomainAssembly = typeof(Ingredient).Assembly;

    [Test]
    public void Domain_ShouldNotDependOn_EntityFramework()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.EntityFrameworkCore")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_AspNetCore()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("Microsoft.AspNetCore")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_HttpClient()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("System.Net.Http")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_DataProject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Data")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_ContractsProject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Contracts")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_ApiProject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Api")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_WebProject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Web")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Domain_ShouldNotDependOn_ServiceDefaultsProject()
    {
        var result = Types.InAssembly(DomainAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.ServiceDefaults")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    private static string FormatFailingTypes(TestResult result)
    {
        if (result.IsSuccessful || result.FailingTypes is null)
        {
            return string.Empty;
        }

        var typeNames = result.FailingTypes.Select(t => t.FullName);
        return $"Violating types: {string.Join(", ", typeNames)}";
    }
}
