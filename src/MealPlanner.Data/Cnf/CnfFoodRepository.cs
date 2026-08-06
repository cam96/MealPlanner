using System.Globalization;
using System.Text;

namespace MealPlanner.Data.Cnf;

/// <summary>
/// File-backed <see cref="ICnfFoodRepository"/> that parses the Canadian Nutrient File (CNF) 2015
/// CSV files. Only the food names and the five tracked nutrients (energy, protein, fibre,
/// carbohydrate, fat) are read; the parsed dataset is cached in memory after the first access.
/// </summary>
public sealed class CnfFoodRepository : ICnfFoodRepository
{
    private const string FoodNameFile = "FOOD NAME.csv";
    private const string NutrientAmountFile = "NUTRIENT AMOUNT.csv";

    /// <summary>CNF nutrient id for energy in kilocalories.</summary>
    private const int EnergyKcalNutrientId = 208;

    /// <summary>CNF nutrient id for protein in grams.</summary>
    private const int ProteinNutrientId = 203;

    /// <summary>CNF nutrient id for total dietary fibre in grams.</summary>
    private const int FibreNutrientId = 291;

    /// <summary>CNF nutrient id for total carbohydrate in grams.</summary>
    private const int CarbNutrientId = 205;

    /// <summary>CNF nutrient id for total fat (lipids) in grams.</summary>
    private const int FatNutrientId = 204;

    private readonly string _directory;
    private readonly Lazy<Dataset> _dataset;

    /// <summary>Initializes a new instance of the <see cref="CnfFoodRepository"/> class.</summary>
    /// <param name="options">The CNF dataset location options.</param>
    public CnfFoodRepository(CnfOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);
        _directory = options.Directory;
        _dataset = new Lazy<Dataset>(Load, LazyThreadSafetyMode.ExecutionAndPublication);
    }

    /// <inheritdoc />
    public bool IsAvailable =>
        File.Exists(Path.Combine(_directory, FoodNameFile))
        && File.Exists(Path.Combine(_directory, NutrientAmountFile));

    /// <inheritdoc />
    public IReadOnlyList<CnfFoodSummary> Search(string query, int maxResults)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(query);
        if (maxResults <= 0 || !IsAvailable)
        {
            return [];
        }

        var trimmedQuery = query.Trim();
        var terms = trimmedQuery.Split(' ', StringSplitOptions.RemoveEmptyEntries);
        return [.. _dataset.Value.Foods
            .Where(f => terms.All(t => f.Description.Contains(t, StringComparison.OrdinalIgnoreCase)))
            .OrderBy(f => f.Description.StartsWith(trimmedQuery, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(f => f.Description.Length)
            .ThenBy(f => f.Description, StringComparer.OrdinalIgnoreCase)
            .Take(maxResults)];
    }

    /// <inheritdoc />
    public CnfFoodNutrition? GetByFoodCode(int foodCode)
    {
        if (!IsAvailable)
        {
            return null;
        }

        return _dataset.Value.ByFoodCode.GetValueOrDefault(foodCode);
    }

    private Dataset Load()
    {
        // FOOD NAME.csv: FoodID, FoodCode, FoodGroupID, FoodSourceID, FoodDescription, ...
        var foods = new List<(int FoodId, int FoodCode, string Description)>();
        var codeByFoodId = new Dictionary<int, int>();
        foreach (var fields in ReadCsv(Path.Combine(_directory, FoodNameFile)))
        {
            if (fields.Count < 5
                || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var foodId)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var foodCode))
            {
                continue;
            }

            var description = fields[4].Trim();
            if (description.Length == 0)
            {
                continue;
            }

            foods.Add((foodId, foodCode, description));
            codeByFoodId[foodId] = foodCode;
        }

        // NUTRIENT AMOUNT.csv: FoodID, NutrientID, NutrientValue, ... (values are per 100 g).
        var nutrientsByFoodId = new Dictionary<int, double[]>();
        foreach (var fields in ReadCsv(Path.Combine(_directory, NutrientAmountFile)))
        {
            if (fields.Count < 3
                || !int.TryParse(fields[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out var foodId)
                || !int.TryParse(fields[1], NumberStyles.Integer, CultureInfo.InvariantCulture, out var nutrientId))
            {
                continue;
            }

            var slot = nutrientId switch
            {
                EnergyKcalNutrientId => 0,
                ProteinNutrientId => 1,
                FibreNutrientId => 2,
                CarbNutrientId => 3,
                FatNutrientId => 4,
                _ => -1,
            };
            if (slot < 0 || !codeByFoodId.ContainsKey(foodId)
                || !double.TryParse(fields[2], NumberStyles.Float, CultureInfo.InvariantCulture, out var value))
            {
                continue;
            }

            if (!nutrientsByFoodId.TryGetValue(foodId, out var values))
            {
                values = new double[5];
                nutrientsByFoodId[foodId] = values;
            }

            values[slot] = value;
        }

        var summaries = new List<CnfFoodSummary>(foods.Count);
        var byFoodCode = new Dictionary<int, CnfFoodNutrition>(foods.Count);
        foreach (var (foodId, foodCode, description) in foods)
        {
            var values = nutrientsByFoodId.GetValueOrDefault(foodId);
            var nutrition = new CnfFoodNutrition(
                foodCode,
                description,
                values?[0] ?? 0,
                values?[1] ?? 0,
                values?[2] ?? 0,
                values?[3] ?? 0,
                values?[4] ?? 0);

            if (byFoodCode.TryAdd(foodCode, nutrition))
            {
                summaries.Add(new CnfFoodSummary(foodCode, description));
            }
        }

        return new Dataset(summaries, byFoodCode);
    }

    /// <summary>
    /// Reads a CNF CSV file, skipping the header row, and yields each data row's fields. The CNF
    /// files use Windows/Latin-1 text and RFC 4180 quoting (commas allowed inside quoted fields).
    /// </summary>
    private static IEnumerable<List<string>> ReadCsv(string path)
    {
        using var reader = new StreamReader(path, Encoding.Latin1);

        // Skip the header line.
        if (reader.ReadLine() is null)
        {
            yield break;
        }

        while (reader.ReadLine() is { } line)
        {
            if (line.Length == 0)
            {
                continue;
            }

            yield return ParseCsvLine(line);
        }
    }

    private static List<string> ParseCsvLine(string line)
    {
        var fields = new List<string>();
        var builder = new StringBuilder();
        var inQuotes = false;

        for (var i = 0; i < line.Length; i++)
        {
            var c = line[i];
            if (inQuotes)
            {
                if (c == '"')
                {
                    if (i + 1 < line.Length && line[i + 1] == '"')
                    {
                        builder.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = false;
                    }
                }
                else
                {
                    builder.Append(c);
                }
            }
            else if (c == '"')
            {
                inQuotes = true;
            }
            else if (c == ',')
            {
                fields.Add(builder.ToString());
                builder.Clear();
            }
            else
            {
                builder.Append(c);
            }
        }

        fields.Add(builder.ToString());
        return fields;
    }

    private sealed record Dataset(
        IReadOnlyList<CnfFoodSummary> Foods,
        IReadOnlyDictionary<int, CnfFoodNutrition> ByFoodCode);
}
