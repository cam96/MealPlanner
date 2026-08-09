using System.Reflection;
using MealPlanner.Data;
using MealPlanner.Domain.Entities;
using NetArchTest.Rules;

namespace MealPlanner.Tests.Architecture;

/// <summary>
/// Enforces allowed and disallowed project references between solution layers.
/// </summary>
[TestFixture]
public class LayerDependencyTests
{
    private static readonly Assembly DomainAssembly = typeof(Ingredient).Assembly;
    private static readonly Assembly DataAssembly = typeof(MealPlannerDbContext).Assembly;
    private static readonly Assembly ContractsAssembly = Assembly.Load("MealPlanner.Contracts");
    private static readonly Assembly ApiAssembly = Assembly.Load("MealPlanner.Api");
    private static readonly Assembly WebAssembly = Assembly.Load("MealPlanner.Web");
    private static readonly Assembly ServiceDefaultsAssembly = Assembly.Load("MealPlanner.ServiceDefaults");

    // ─── Data layer: allowed → Domain only ───────────────────────────────────

    [Test]
    public void Data_ShouldNotDependOn_ContractsProject()
    {
        var result = Types.InAssembly(DataAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Contracts")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Data_ShouldNotDependOn_ApiProject()
    {
        var result = Types.InAssembly(DataAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Api")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Data_ShouldNotDependOn_WebProject()
    {
        var result = Types.InAssembly(DataAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Web")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Data_ShouldNotDependOn_ServiceDefaultsProject()
    {
        var result = Types.InAssembly(DataAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.ServiceDefaults")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    // ─── Contracts layer: zero project dependencies ──────────────────────────

    [Test]
    public void Contracts_ShouldNotDependOn_DomainProject()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Domain")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Contracts_ShouldNotDependOn_DataProject()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Data")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Contracts_ShouldNotDependOn_ApiProject()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Api")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Contracts_ShouldNotDependOn_WebProject()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Web")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Contracts_ShouldNotDependOn_ServiceDefaultsProject()
    {
        var result = Types.InAssembly(ContractsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.ServiceDefaults")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    // ─── Api layer: allowed → Domain, Data, Contracts, ServiceDefaults ───────

    [Test]
    public void Api_ShouldNotDependOn_WebProject()
    {
        var result = Types.InAssembly(ApiAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Web")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    // ─── Web layer: allowed → Contracts, ServiceDefaults only ────────────────

    [Test]
    public void Web_ShouldNotDependOn_DomainProject()
    {
        var result = Types.InAssembly(WebAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Domain")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Web_ShouldNotDependOn_DataProject()
    {
        var result = Types.InAssembly(WebAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Data")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void Web_ShouldNotDependOn_ApiProject()
    {
        var result = Types.InAssembly(WebAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Api")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    // ─── ServiceDefaults: zero project dependencies ──────────────────────────

    [Test]
    public void ServiceDefaults_ShouldNotDependOn_DomainProject()
    {
        var result = Types.InAssembly(ServiceDefaultsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Domain")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void ServiceDefaults_ShouldNotDependOn_DataProject()
    {
        var result = Types.InAssembly(ServiceDefaultsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Data")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void ServiceDefaults_ShouldNotDependOn_ContractsProject()
    {
        var result = Types.InAssembly(ServiceDefaultsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Contracts")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void ServiceDefaults_ShouldNotDependOn_ApiProject()
    {
        var result = Types.InAssembly(ServiceDefaultsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Api")
            .GetResult();

        Assert.That(result.IsSuccessful, Is.True, FormatFailingTypes(result));
    }

    [Test]
    public void ServiceDefaults_ShouldNotDependOn_WebProject()
    {
        var result = Types.InAssembly(ServiceDefaultsAssembly)
            .ShouldNot()
            .HaveDependencyOn("MealPlanner.Web")
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
