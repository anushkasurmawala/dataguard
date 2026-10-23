using System.Globalization;
using DataGuard.Api.DTOs;
using DataGuard.Api.Statistics;

namespace DataGuard.Api.Services;

public record ParsedDataset(string[] Headers, List<string[]> Rows);

public class ProfilingService
{
    public static bool Number(string text, out double number) =>
        double.TryParse(text, NumberStyles.Float, CultureInfo.InvariantCulture, out number) && double.IsFinite(number);

    public static double[] Values(ParsedDataset dataset, int columnIndex) => dataset.Rows
        .Select(row => row[columnIndex])
        .Where(value => !string.IsNullOrWhiteSpace(value))
        .Select(value => double.Parse(value, CultureInfo.InvariantCulture)).ToArray();

    private static string InferType(string[] values)
    {
        if (values.Length == 0) return "unknown";
        if (values.All(value => Number(value, out _))) return "numeric";
        if (values.All(value => bool.TryParse(value, out _))) return "boolean";
        if (values.All(value => DateOnly.TryParseExact(value, "yyyy-MM-dd", CultureInfo.InvariantCulture,
            DateTimeStyles.None, out _))) return "date";
        return "string";
    }

    public DatasetProfile Profile(ParsedDataset dataset)
    {
        var features = new List<FeatureProfile>();
        for (var index = 0; index < dataset.Headers.Length; index++)
        {
            var values = dataset.Rows.Select(row => row[index])
                .Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
            var missing = dataset.Rows.Count - values.Length;
            var type = InferType(values);
            var statistics = type == "numeric" ? SummaryCalculator.Calculate(Values(dataset, index)) : null;
            var categories = type == "numeric" ? null
                : values.GroupBy(value => value).ToDictionary(group => group.Key, group => group.Count());
            features.Add(new FeatureProfile(dataset.Headers[index], type, missing,
                100.0 * missing / dataset.Rows.Count, values.Distinct().Count(), statistics, categories));
        }

        // Serialize the whole row to avoid collisions from joining fields with a delimiter.
        var duplicates = dataset.Rows.Count
            - dataset.Rows.Select(row => System.Text.Json.JsonSerializer.Serialize(row)).Distinct().Count();
        var totalMissing = features.Sum(feature => feature.MissingCount);
        return new DatasetProfile(dataset.Rows.Count, dataset.Headers.Length, duplicates,
            100.0 * duplicates / dataset.Rows.Count, totalMissing,
            100.0 * totalMissing / (dataset.Rows.Count * dataset.Headers.Length), features);
    }
}
