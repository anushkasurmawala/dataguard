using CsvHelper;
using CsvHelper.Configuration;
using System.Globalization;
using System.Text.Json;
using DataGuard.Api.Data;
using DataGuard.Api.DTOs;
using DataGuard.Api.Models;

namespace DataGuard.Api.Services;

public class DatasetService(AppDbContext db, ProfilingService profiling, IWebHostEnvironment env, IConfiguration configuration)
{
    public async Task<ParsedDataset> Parse(string path)
    {
        using var reader = new StreamReader(path);
        return await ParseReader(reader);
    }

    public async Task<ParsedDataset> ParseContent(string content)
    {
        using var reader = new StringReader(content);
        return await ParseReader(reader);
    }

    private async Task<ParsedDataset> ParseReader(TextReader reader)
    {
        using var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            IgnoreBlankLines = true,
            TrimOptions = TrimOptions.Trim
        });

        try
        {
            if (!await csv.ReadAsync())
                throw new ArgumentException("CSV is empty.");
            csv.ReadHeader();
            var headers = csv.HeaderRecord!;
            if (headers.Length > 200 || headers.Any(string.IsNullOrWhiteSpace)
                || headers.Distinct().Count() != headers.Length)
                throw new ArgumentException("Use 1–200 unique, nonempty headers.");

            var rows = new List<string[]>();
            while (await csv.ReadAsync())
            {
                if (csv.Parser.Count != headers.Length)
                    throw new ArgumentException("A CSV row has a different number of fields than the header.");
                rows.Add(Enumerable.Range(0, headers.Length).Select(index => csv.GetField(index) ?? "").ToArray());
                if (rows.Count > 100000)
                    throw new ArgumentException("Prototype limit: 100,000 rows.");
            }
            if (rows.Count == 0)
                throw new ArgumentException("CSV must include data rows.");
            return new ParsedDataset(headers, rows);
        }
        catch (CsvHelperException)
        {
            throw new ArgumentException("Malformed CSV. Check quoting and field separators.");
        }
    }

    public async Task<DatasetResponse> Upload(IFormFile file, string type)
    {
        if (file == null || file.Length == 0 || file.Length > 20 * 1024 * 1024
            || !Path.GetExtension(file.FileName).Equals(".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Choose a nonempty CSV file up to 20 MB.");

        // PostgreSQL retains the original CSV so cloud restarts cannot break comparisons.
        using var reader = new StreamReader(file.OpenReadStream());
        var content = await reader.ReadToEndAsync();
        var profile = profiling.Profile(await ParseContent(content));
        var keepFiles = configuration.GetValue("DatasetStorage:KeepFiles", true);
        var path = "";
        if (keepFiles)
        {
            var folder = Path.Combine(env.ContentRootPath, "uploads");
            Directory.CreateDirectory(folder);
            path = Path.Combine(folder, Guid.NewGuid() + ".csv");
        }
        try
        {
            if (keepFiles)
                await File.WriteAllTextAsync(path, content);
            var dataset = new Dataset
            {
                FileName = Path.GetFileName(file.FileName),
                DatasetType = type,
                FilePath = path,
                CsvContent = content,
                ProfileJson = JsonSerializer.Serialize(profile)
            };
            db.Datasets.Add(dataset);
            await db.SaveChangesAsync();
            return Response(dataset);
        }
        catch
        {
            // Avoid orphaned files if parsing or persistence fails.
            if (path.Length > 0) File.Delete(path);
            throw;
        }
    }

    public static DatasetResponse Response(Dataset dataset) => new(dataset.Id, dataset.FileName,
        dataset.DatasetType, JsonSerializer.Deserialize<DatasetProfile>(dataset.ProfileJson)!);
}
