using System.Text.Json;

using DataGuard.Api.Data;

using DataGuard.Api.DTOs;

using DataGuard.Api.Models;

using DataGuard.Api.Statistics;


namespace DataGuard.Api.Services;


public class ComparisonService(AppDbContext db, DatasetService datasets, PsiCalculator psi)
{

    public static List<SchemaChange> CompareSchema(DatasetProfile baseline, DatasetProfile current)
    {

        var changes = new List<SchemaChange>();

        foreach (var feature in baseline.Features)
        {

            var match = current.Features.Find(x => x.FeatureName == feature.FeatureName);

            if (match == null)
                changes.Add(new(feature.FeatureName, "Removed", feature.DataType, null));

            else if (match.DataType != feature.DataType)
                changes.Add(new(feature.FeatureName, "Type changed", feature.DataType, match.DataType));

        }

        foreach (var feature in current.Features.Where(f => !baseline.Features.Any(x => x.FeatureName == f.FeatureName)))
            changes.Add(new(feature.FeatureName, "Added", null, feature.DataType));

        return changes;

    }


    public async Task<ComparisonResult> Compare(CompareRequest request)
    {

        var baseline = await db.Datasets.FindAsync(request.BaselineDatasetId)
            ?? throw new KeyNotFoundException("Baseline not found.");

        var current = await db.Datasets.FindAsync(request.CurrentDatasetId)
            ?? throw new KeyNotFoundException("Current dataset not found.");

        if (baseline.DatasetType != "baseline" || current.DatasetType != "current")
            throw new ArgumentException("Select baseline and current datasets in the correct roles.");


        var baselineResponse = DatasetService.Response(baseline);

        var currentResponse = DatasetService.Response(current);

        var baselineData = await datasets.Parse(baseline.FilePath);

        var currentData = await datasets.Parse(current.FilePath);

        var features = new List<FeatureResult>();


        foreach (var feature in baselineResponse.Profile.Features)
        {

            var match = currentResponse.Profile.Features.Find(x => x.FeatureName == feature.FeatureName);

            if (match == null)
                continue;


            var idLike = feature.FeatureName.Equals("id", StringComparison.OrdinalIgnoreCase)
                || feature.FeatureName.EndsWith("_id", StringComparison.OrdinalIgnoreCase)
                || feature.FeatureName.EndsWith("Id", StringComparison.Ordinal);

            var reason = idLike ? "ID-like feature"
                : feature.DataType != "numeric" || match.DataType != "numeric"
                    ? "PSI supports common numerical features only" : null;


            PsiResult? drift = null;

            if (reason == null)
            {

                var baselineValues = ProfilingService.Values(baselineData, Array.IndexOf(baselineData.Headers, feature.FeatureName));

                var currentValues = ProfilingService.Values(currentData, Array.IndexOf(currentData.Headers, feature.FeatureName));

                drift = psi.Calculate(baselineValues, currentValues);

            }

            features.Add(new(feature.FeatureName, feature.MissingPercentage, match.MissingPercentage,
                match.MissingPercentage - feature.MissingPercentage, feature.Numeric, match.Numeric, drift, reason));

        }


        var entity = new Comparison
        {
            BaselineDatasetId = baseline.Id,
            CurrentDatasetId = current.Id
        }
;

        var result = new ComparisonResult(entity.Id, baselineResponse, currentResponse,
            CompareSchema(baselineResponse.Profile, currentResponse.Profile), features);

        entity.ResultJson = JsonSerializer.Serialize(result);

        db.Comparisons.Add(entity);

        await db.SaveChangesAsync();

        return result;

    }

}

