using Microsoft.EntityFrameworkCore;
using DataGuard.Api.Models;
namespace DataGuard.Api.Data;
public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
    public DbSet<Dataset> Datasets => Set<Dataset>();
    public DbSet<Comparison> Comparisons => Set<Comparison>();
    protected override void OnModelCreating(ModelBuilder b)
    {
        b.Entity<Dataset>().Property(x => x.ProfileJson).HasColumnType("jsonb");
        b.Entity<Comparison>().Property(x => x.ResultJson).HasColumnType("jsonb");
        b.Entity<Comparison>().HasOne(x => x.BaselineDataset).WithMany().HasForeignKey(x => x.BaselineDatasetId).OnDelete(DeleteBehavior.Restrict);
        b.Entity<Comparison>().HasOne(x => x.CurrentDataset).WithMany().HasForeignKey(x => x.CurrentDatasetId).OnDelete(DeleteBehavior.Restrict);
    }
}
