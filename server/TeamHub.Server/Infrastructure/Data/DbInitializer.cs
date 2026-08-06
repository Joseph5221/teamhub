using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Infrastructure.Data;

/// <summary>
/// Dev-time test data. Runs at startup (Development only, see Program.cs)
/// and via POST /api/dev/reseed. Idempotent — skips if any user already
/// exists, so it's safe to call more than once against the same context.
/// </summary>
public static class DbInitializer
{
    public static async Task SeedAsync(AppDbContext context)
    {
        if (await context.Users.AnyAsync())
        {
            return;
        }

        var alice = new User
        {
            Id = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            Email = "test@teamhub.com",
            Name = "Test User",
            PasswordHash = "temporary", // any password works — see AuthService TODO
            Role = "Admin"
        };
        var bob = new User
        {
            Email = "bob@teamhub.com",
            Name = "Bob Builder",
            PasswordHash = "temporary",
            Role = "User"
        };
        var carol = new User
        {
            Email = "carol@teamhub.com",
            Name = "Carol Danvers",
            PasswordHash = "temporary",
            Role = "User"
        };
        context.Users.AddRange(alice, bob, carol);

        var platformTeam = new Team
        {
            Name = "Platform",
            Description = "Core platform & infrastructure",
            Owner = alice,
            Members = { alice, bob }
        };
        var growthTeam = new Team
        {
            Name = "Growth",
            Description = "Product growth & integrations",
            Owner = carol,
            Members = { carol, bob }
        };
        context.Teams.AddRange(platformTeam, growthTeam);

        context.Projects.AddRange(
            new Project
            {
                Name = "Dashboard Revamp",
                Description = "Rebuild the team dashboard UI",
                Status = "Active",
                Team = platformTeam,
                StartDate = DateTime.UtcNow.AddDays(-30)
            },
            new Project
            {
                Name = "Auth Hardening",
                Description = "Replace dev-only auth with real hashing/refresh tokens",
                Status = "Active",
                Team = platformTeam,
                StartDate = DateTime.UtcNow.AddDays(-10)
            },
            new Project
            {
                Name = "Jira Sync",
                Description = "Pull issues from Jira into TeamHub",
                Status = "Planned",
                Team = growthTeam
            }
        );

        context.Integrations.AddRange(
            new Integration { Name = "GitHub", Type = IntegrationType.VersionControl, Status = IntegrationStatus.Todo, Description = "Connect your GitHub repositories", Team = platformTeam },
            new Integration { Name = "Jira", Type = IntegrationType.ProjectManagement, Status = IntegrationStatus.Todo, Description = "Sync issues and tickets from Jira", Team = platformTeam },
            new Integration { Name = "Slack", Type = IntegrationType.Communication, Status = IntegrationStatus.Todo, Description = "Get notifications in Slack", Team = platformTeam },
            new Integration { Name = "GitHub", Type = IntegrationType.VersionControl, Status = IntegrationStatus.Connected, Description = "Connect your GitHub repositories", Team = growthTeam, LastSyncedAt = DateTime.UtcNow.AddHours(-2) }
        );

        await context.SaveChangesAsync();
    }
}
