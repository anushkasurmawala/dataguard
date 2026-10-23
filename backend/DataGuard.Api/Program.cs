using DataGuard.Api.Data;
using DataGuard.Api.Services;
using DataGuard.Api.Statistics;
using Microsoft.EntityFrameworkCore;

// File watching is unnecessary for this local prototype and can hang in restricted environments.
Environment.SetEnvironmentVariable("DOTNET_HOSTBUILDER__RELOADCONFIGONCHANGE", "false");
var builder = WebApplication.CreateBuilder(args);

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrEmpty(port)) builder.WebHost.UseUrls($"http://0.0.0.0:{port}");
builder.Services.AddControllers();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(DatabaseConnection.Resolve(builder.Configuration["DATABASE_URL"], builder.Configuration.GetConnectionString("DataGuard"))));
builder.Services.AddScoped<DatasetService>();
builder.Services.AddScoped<ProfilingService>();
builder.Services.AddScoped<ComparisonService>();
builder.Services.AddSingleton<PsiCalculator>();
builder.Services.AddCors(options => options.AddDefaultPolicy(policy =>
    policy.WithOrigins("http://localhost:4200", "http://127.0.0.1:4200")
        .AllowAnyHeader().AllowAnyMethod()));

var app = builder.Build();
if (builder.Configuration.GetValue<bool>("Database:ApplyMigrations"))
{
    using var scope = app.Services.CreateScope();
    await scope.ServiceProvider.GetRequiredService<AppDbContext>().Database.MigrateAsync();
}
app.Use(async (context, next) =>
{
    try
    {
        await next();
    }
    catch (Exception exception)
    {
        var status = exception is ArgumentException ? 400 : exception is KeyNotFoundException ? 404 : 500;
        app.Logger.LogError(exception, "Request failed");
        context.Response.StatusCode = status;
        await context.Response.WriteAsJsonAsync(new
        {
            message = status == 500
                ? "Request failed. Check that PostgreSQL is running and migrations are applied."
                : exception.Message
        });
    }
});
app.UseCors();
app.MapControllers();
app.MapGet("/api/health", () => new { status = "ok" });
app.Run();
