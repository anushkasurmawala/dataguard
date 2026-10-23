using DataGuard.Api.DTOs;

namespace DataGuard.Api.Statistics;

public static class SummaryCalculator
{
    // Linear interpolation between adjacent sorted observations.
    public static double Quantile(double[] sorted, double probability)
    {
        var position = (sorted.Length - 1) * probability;
        var lower = (int)position;
        var upper = (int)Math.Ceiling(position);
        return sorted[lower] + (sorted[upper] - sorted[lower]) * (position - lower);
    }

    public static NumericSummary Calculate(double[] values)
    {
        var sorted = values.Order().ToArray();
        var mean = sorted.Average();
        var q1 = Quantile(sorted, .25);
        var q3 = Quantile(sorted, .75);
        var iqr = q3 - q1;
        var outliers = sorted.Count(value => value < q1 - 1.5 * iqr || value > q3 + 1.5 * iqr);
        var standardDeviation = Math.Sqrt(sorted.Average(value => Math.Pow(value - mean, 2)));

        return new NumericSummary(mean, Quantile(sorted, .5), standardDeviation,
            sorted[0], sorted[^1], q1, q3, outliers, 100.0 * outliers / sorted.Length);
    }
}

public class PsiCalculator
{
    public PsiResult Calculate(double[] baseline, double[] current)
    {
        if (baseline.Length == 0 || current.Length == 0)
            throw new ArgumentException("PSI requires non-missing values in both datasets.");

        var sorted = baseline.Order().ToArray();
        var constant = sorted[0] == sorted[^1];
        // Only baseline values define the cuts; both datasets share them.
        var cuts = Enumerable.Range(1, 9)
            .Select(i => SummaryCalculator.Quantile(sorted, i / 10.0))
            .Distinct()
            .Where(value => value > sorted[0] && value < sorted[^1])
            .ToArray();
        if (cuts.Length == 0)
            cuts = new[] { sorted[0] };

        int[] CountBins(double[] values)
        {
            // A constant baseline needs an equality bin to detect either direction of change.
            if (constant)
                return new[] { values.Count(x => x < sorted[0]), values.Count(x => x == sorted[0]), values.Count(x => x > sorted[0]) };

            var counts = new int[cuts.Length + 1];
            foreach (var value in values)
            {
                var index = 0;
                while (index < cuts.Length && value > cuts[index])
                    index++;
                counts[index]++;
            }
            return counts;
        }

        var baselineCounts = CountBins(baseline);
        var currentCounts = CountBins(current);
        var bins = new List<DistributionBin>();
        double psi = 0;
        const double epsilon = 1e-6;
        var binCount = baselineCounts.Length;

        for (var index = 0; index < binCount; index++)
        {
            var baselineRaw = baselineCounts[index] / (double)baseline.Length;
            var currentRaw = currentCounts[index] / (double)current.Length;
            // Additive smoothing keeps every proportion positive and the totals normalized.
            var baselineProportion = (baselineRaw + epsilon) / (1 + epsilon * binCount);
            var currentProportion = (currentRaw + epsilon) / (1 + epsilon * binCount);
            psi += (currentProportion - baselineProportion) * Math.Log(currentProportion / baselineProportion);

            string label;
            if (constant)
                label = index == 0 ? $"< {sorted[0]:G5}" : index == 1 ? $"= {sorted[0]:G5}" : $"> {sorted[0]:G5}";
            else if (index == 0)
                label = $"≤ {cuts[0]:G5}";
            else if (index == cuts.Length)
                label = $"> {cuts[^1]:G5}";
            else
                label = $"({cuts[index - 1]:G5}, {cuts[index]:G5}]";

            bins.Add(new DistributionBin(label, 100 * baselineRaw, 100 * currentRaw));
        }

        var status = psi < .10 ? "Stable" : psi < .25 ? "Moderate" : "Significant";
        return new PsiResult(psi, status, cuts, bins);
    }
}
