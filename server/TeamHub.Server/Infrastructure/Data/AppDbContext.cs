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

        // Seed initial data for development
        SeedData(modelBuilder);
    }

    private void SeedData(ModelBuilder modelBuilder)
    {
        // Seed a test user for development
        var testUserId = Guid.Parse("11111111-1111-1111-1111-111111111111");
        modelBuilder.Entity<User>().HasData(new User
        {
            Id = testUserId,
            Email = "test@teamhub.com",
            Name = "Test User",
            PasswordHash = "temporary", // Will be replaced with real hashing later
            Role = "Admin",
            CreatedAt = DateTime.UtcNow
        });

        // Seed sample integrations as TODO items
        var integrationId1 = Guid.Parse("22222222-2222-2222-2222-222222222222");
        var integrationId2 = Guid.Parse("33333333-3333-3333-3333-333333333333");
        var integrationId3 = Guid.Parse("44444444-4444-4444-4444-444444444444");

        modelBuilder.Entity<Integration>().HasData(
            new Integration
            {
                Id = integrationId1,
                Name = "GitHub",
                Type = "VersionControl",
                Status = "TODO",
                Description = "Connect your GitHub repositories",
                UserId = testUserId,
                CreatedAt = DateTime.UtcNow
            },
            new Integration
            {
                Id = integrationId2,
                Name = "Jira",
                Type = "ProjectManagement",
                Status = "TODO",
                Description = "Sync issues and tickets from Jira",
                UserId = testUserId,
                CreatedAt = DateTime.UtcNow
            },
            new Integration
            {
                Id = integrationId3,
                Name = "Slack",
                Type = "Communication",
                Status = "TODO",
                Description = "Get notifications in Slack",
                UserId = testUserId,
                CreatedAt = DateTime.UtcNow
            }
        );
    }
}