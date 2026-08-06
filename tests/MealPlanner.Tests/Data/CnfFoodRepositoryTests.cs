using System.Text;
using MealPlanner.Data.Cnf;

namespace MealPlanner.Tests.Data;

/// <summary>
/// Verifies <see cref="CnfFoodRepository"/> parses the Canadian Nutrient File CSV files, maps the
/// tracked nutrients, and reports availability correctly.
/// </summary>
[TestFixture]
public class CnfFoodRepositoryTests
{
    private string _directory = null!;

    [SetUp]
    public void SetUp()
    {
        _directory = Path.Combine(Path.GetTempPath(), $"mealplanner-cnf-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_directory);
    }

    [TearDown]
    public void TearDown()
    {
        try
        {
            Directory.Delete(_directory, recursive: true);
        }
        catch (IOException)
        {
            // Best-effort cleanup of a throwaway temp directory.
        }
    }

    [Test]
    public void IsAvailable_IsFalse_WhenFilesMissing()
    {
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        Assert.That(repository.IsAvailable, Is.False);
    }

    [Test]
    public void Search_FindsFood_AndPopulatesTrackedNutrients()
    {
        WriteSampleDataset();
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        var results = repository.Search("chop", maxResults: 10);
        var nutrition = repository.GetByFoodCode(4);

        Assert.Multiple(() =>
        {
            Assert.That(results, Has.Count.EqualTo(1));
            Assert.That(results[0].FoodCode, Is.EqualTo(4));
            Assert.That(results[0].Description, Is.EqualTo("Chop suey, with meat, canned"));
            Assert.That(nutrition, Is.Not.Null);
            Assert.That(nutrition!.CaloriesPer100, Is.EqualTo(120).Within(0.001));
            Assert.That(nutrition.ProteinPer100, Is.EqualTo(9.54).Within(0.001));
            Assert.That(nutrition.FiberPer100, Is.EqualTo(1.2).Within(0.001));
            Assert.That(nutrition.CarbsPer100, Is.EqualTo(20.3).Within(0.001));
            Assert.That(nutrition.FatPer100, Is.EqualTo(4.5).Within(0.001));
        });
    }

    [Test]
    public void Search_MultipleWords_RequiresAllWordsToMatch()
    {
        WriteSampleDataset();
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        // "chop meat" should match "Chop suey, with meat, canned" (contains both words)
        var matchingResults = repository.Search("chop meat", maxResults: 10);
        // "chop cheese" should not match anything (no item contains both)
        var noResults = repository.Search("chop cheese", maxResults: 10);

        Assert.Multiple(() =>
        {
            Assert.That(matchingResults, Has.Count.EqualTo(1));
            Assert.That(matchingResults[0].Description, Is.EqualTo("Chop suey, with meat, canned"));
            Assert.That(noResults, Is.Empty);
        });
    }

    [Test]
    public void GetByFoodCode_DefaultsMissingNutrientsToZero()
    {
        WriteSampleDataset();
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        var nutrition = repository.GetByFoodCode(2);

        Assert.Multiple(() =>
        {
            Assert.That(nutrition, Is.Not.Null);
            Assert.That(nutrition!.ProteinPer100, Is.EqualTo(9.54).Within(0.001));
            Assert.That(nutrition.FatPer100, Is.EqualTo(15.7).Within(0.001));
            Assert.That(nutrition.CaloriesPer100, Is.EqualTo(0));
            Assert.That(nutrition.FiberPer100, Is.EqualTo(0));
            Assert.That(nutrition.CarbsPer100, Is.EqualTo(0));
        });
    }

    [Test]
    public void GetByFoodCode_ReturnsNull_WhenUnknown()
    {
        WriteSampleDataset();
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        Assert.That(repository.GetByFoodCode(999), Is.Null);
    }

    [Test]
    public void Search_ReturnsEmpty_WhenDatasetUnavailable()
    {
        var repository = new CnfFoodRepository(new CnfOptions { Directory = _directory });

        Assert.That(repository.Search("chop", maxResults: 10), Is.Empty);
    }

    private void WriteSampleDataset()
    {
        // Food 2 has protein and fat only; food 4 has all five tracked nutrients and a quoted,
        // comma-bearing description to exercise RFC 4180 parsing.
        File.WriteAllText(
            Path.Combine(_directory, "FOOD NAME.csv"),
            "FoodID,FoodCode,FoodGroupID,FoodSourceID,FoodDescription\n"
            + "2,2,22,20,Cheese souffle\n"
            + "4,4,22,20,\"Chop suey, with meat, canned\"\n",
            Encoding.Latin1);

        File.WriteAllText(
            Path.Combine(_directory, "NUTRIENT AMOUNT.csv"),
            "FoodID,NutrientID,NutrientValue,StandardError,NumberofObservations\n"
            + "2,203,9.54,0,0\n"
            + "2,204,15.7,0,0\n"
            + "4,203,9.54,0,0\n"
            + "4,204,4.5,0,0\n"
            + "4,205,20.3,0,0\n"
            + "4,208,120,0,0\n"
            + "4,291,1.2,0,0\n",
            Encoding.Latin1);
    }
}
