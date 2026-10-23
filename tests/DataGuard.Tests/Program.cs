using DataGuard.Api.Services;
using DataGuard.Api.Statistics;
using DataGuard.Api.DTOs;
var failed = 0; var passed = 0;
void Check(string name, Action test) { try { test(); Console.WriteLine($"PASS {name}"); passed++; } catch (Exception e) { Console.WriteLine($"FAIL {name}: {e.Message}"); failed++; } }
void Assert(bool condition) { if (!condition) throw new Exception("Assertion failed"); }
var psi = new PsiCalculator(); var original = Enumerable.Range(1, 100).Select(x => (double)x).ToArray();
Check("PSI: identical distributions", () => Assert(Math.Abs(psi.Calculate(original, original).Psi) < 1e-12));
Check("PSI: shifted distributions", () => Assert(psi.Calculate(original, original.Select(x => x + 70).ToArray()).Status == "Significant"));
Check("PSI: zero-frequency bins remain finite", () => Assert(double.IsFinite(psi.Calculate(original, new double[] { 1000, 1000 }).Psi)));
Check("PSI: baseline cuts shared by both datasets", () => { var p = psi.Calculate(original, new double[] { -1000, 1000 }); Assert(p.Bins.First().CurrentPercentage == 50 && p.Bins.Last().CurrentPercentage == 50); Assert(p.Boundaries.Length == 9); });
Check("PSI: constant baseline supports changed values", () => { var p = psi.Calculate(new double[] { 5, 5, 5 }, new double[] { 5, 6, 6 }); Assert(double.IsFinite(p.Psi) && p.Psi > 0); });
Check("PSI: tied quantiles produce unique boundaries", () => { var p = psi.Calculate(new double[] { 1, 1, 1, 1, 2, 2, 2, 3, 3, 3 }, new double[] { 1, 2, 3 }); Assert(p.Boundaries.Distinct().Count() == p.Boundaries.Length); });
Check("IQR: ordinary values", () => Assert(SummaryCalculator.Calculate(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 }).OutlierCount == 0));
Check("IQR: extreme value", () => Assert(SummaryCalculator.Calculate(new double[] { 1, 2, 3, 4, 5, 6, 7, 8, 100 }).OutlierCount == 1));
Check("Summary: mean, median and population SD", () => { var s = SummaryCalculator.Calculate(new double[] { 1, 2, 3 }); Assert(s.Mean == 2 && s.Median == 2 && Math.Abs(s.StandardDeviation - Math.Sqrt(2.0 / 3)) < 1e-10); });
var profiler = new ProfilingService();
DatasetProfile Profile(string[] headers, params string[][] rows) => profiler.Profile(new(headers, rows.ToList()));
Check("Schema: added and removed columns", () => { var changes = ComparisonService.CompareSchema(Profile(new[] { "a", "removed" }, new[] { "1", "2" }), Profile(new[] { "a", "added" }, new[] { "2", "3" })); Assert(changes.Any(x => x.ColumnName == "added" && x.ChangeType == "Added") && changes.Any(x => x.ColumnName == "removed" && x.ChangeType == "Removed")); });
Check("Schema: changed datatype", () => { var changes = ComparisonService.CompareSchema(Profile(new[] { "a" }, new[] { "1" }), Profile(new[] { "a" }, new[] { "text" })); Assert(changes.Single().ChangeType == "Type changed"); });
Check("Profile: duplicates and whitespace missingness", () => { var p = Profile(new[] { "a", "b" }, new[] { "1", " " }, new[] { "1", " " }, new[] { "2", "x" }); Assert(p.DuplicateRowCount == 1 && p.TotalMissingValues == 2 && p.Features[0].Numeric!.Mean == 4.0 / 3); });
Check("Profile: nonfinite values are not numeric", () => Assert(Profile(new[] { "a" }, new[] { "NaN" }).Features[0].DataType == "string"));
Check("Profile: all-missing feature is unknown", () => Assert(Profile(new[] { "a" }, new[] { "" }).Features[0].DataType == "unknown"));
Check("PSI: moderate change classification", () => Assert(psi.Calculate(original, original.Concat(Enumerable.Repeat(100d, 20)).ToArray()).Status == "Moderate"));
Check("PSI: constant baseline detects a downward shift", () => Assert(psi.Calculate(new double[] { 5, 5, 5 }, new double[] { 4, 4, 4 }).Status == "Significant"));
Check("Database: hosted URL conversion", () => {
    var connection = new Npgsql.NpgsqlConnectionStringBuilder(DataGuard.Api.Data.DatabaseConnection.Resolve("postgres://student:p%40ss@db.example.test:5433/dataguard", null));
    Assert(connection.Host == "db.example.test" && connection.Port == 5433 && connection.Password == "p@ss" && connection.Database == "dataguard");
});
Check("Database: configured connection preserved", () => Assert(DataGuard.Api.Data.DatabaseConnection.Resolve(null, "Host=localhost;Database=dataguard") == "Host=localhost;Database=dataguard"));
Console.WriteLine($"{passed} passed; {failed} failed."); return failed == 0 ? 0 : 1;
