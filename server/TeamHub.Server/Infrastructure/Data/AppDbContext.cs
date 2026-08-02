using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;

namespace TeamHub.Server.Infrastructure.Data;

/// <summary>
/// Database context for TeamHub application
/// </summary>
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
    {
    }

    public DbSet<User> Users { get; set; } = null!;
    public DbSet<Team> Teams { get; set; } = null!;
    public DbSet<Project> Projects { get; set; } = null!;
    public DbSet<Integration> Integrations { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Apply configurations from separate configuration classes
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

        // Dev test data lives in DbInitializer.SeedAsync (runtime, not
        // model-build-time) — HasData can't express relational data like
        // Team.Owner/Members cleanly. See Program.cs for where it's called.
    }
}