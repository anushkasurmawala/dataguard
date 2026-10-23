using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
namespace DataGuard.Api.Data;
public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        var connection = DatabaseConnection.Resolve(Environment.GetEnvironmentVariable("DATABASE_URL"),
            Environment.GetEnvironmentVariable("ConnectionStrings__DataGuard") ?? "Host=localhost;Port=5432;Database=dataguard;Username=dataguard;Password=dataguard");
        return new(new DbContextOptionsBuilder<AppDbContext>().UseNpgsql(connection).Options);
    }
}
