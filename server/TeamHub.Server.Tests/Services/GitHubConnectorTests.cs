// TeamHub.Server.Tests/Services/GitHubConnectorTests.cs
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Modules.Integrations;
using TeamHub.Server.Modules.Integrations.GitHub;
using TeamHub.Server.Infrastructure.Data;

public class GitHubConnectorTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IGitHubApiClient> _mockApiClient;
    private readonly GitHubConnector _sut;

    public GitHubConnectorTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _mockApiClient = new Mock<IGitHubApiClient>();
        _sut = new GitHubConnector(_context, _mockApiClient.Object);
    }

    private async Task<(Team team, Integration integration)> SeedTeamWithGitHubIntegrationAsync(string? configJson)
    {
        var owner = new User { Email = "owner@test.com", Name = "Owner" };
        var team = new Team { Name = "Growth", Owner = owner, Members = { owner } };
        var integration = new Integration
        {
            Name = "GitHub",
            Type = IntegrationType.VersionControl,
            Status = IntegrationStatus.Todo,
            Team = team,
            ConfigurationData = configJson
        };
        _context.Teams.Add(team);
        _context.Integrations.Add(integration);
        await _context.SaveChangesAsync();
        return (team, integration);
    }

    [Fact]
    public async Task GetConfigAsync_WithNoIntegration_ReturnsNotConfigured()
    {
        var config = await _sut.GetConfigAsync(Guid.NewGuid());

        config.IsConfigured.Should().BeFalse();
    }

    [Fact]
    public async Task GetConfigAsync_WithOrganizationConfigured_ReturnsConfiguredWithoutLeakingToken()
    {
        var (team, _) = await SeedTeamWithGitHubIntegrationAsync(
            """{"organization":"octokit","personalAccessToken":"secret-token"}""");

        var config = await _sut.GetConfigAsync(team.Id);

        config.IsConfigured.Should().BeTrue();
        config.Fields["organization"].Should().Be("octokit");
        config.Fields["hasPersonalAccessToken"].Should().Be("True");
        config.Fields.Values.Should().NotContain("secret-token");
    }

    [Fact]
    public async Task GetDataAsync_WithNoOrganizationConfigured_ThrowsNotConfigured()
    {
        var (team, _) = await SeedTeamWithGitHubIntegrationAsync(null);

        var act = () => _sut.GetDataAsync(team.Id);

        var ex = await act.Should().ThrowAsync<ModuleConnectorException>();
        ex.Which.Code.Should().Be("GitHub.NotConfigured");
    }

    [Fact]
    public async Task GetDataAsync_WithOrganizationConfigured_ReturnsMappedModuleData()
    {
        var (team, _) = await SeedTeamWithGitHubIntegrationAsync("""{"organization":"octokit"}""");

        _mockApiClient
            .Setup(c => c.GetOrganizationRepositoriesAsync("octokit", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GitHubRepositoryDto>
            {
                new(1, "octokit.net", "octokit/octokit.net", "A .NET client", "https://github.com/octokit/octokit.net",
                    "C#", 2500, 42, new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc), false)
            });

        var data = await _sut.GetDataAsync(team.Id);

        data.Items.Should().ContainSingle();
        data.Items[0].Title.Should().Be("octokit/octokit.net");
        data.Items[0].Metadata!["language"].Should().Be("C#");
    }

    [Fact]
    public async Task InvokeActionAsync_WithSync_UpdatesStatusAndLastSyncedAt()
    {
        var (team, integration) = await SeedTeamWithGitHubIntegrationAsync("""{"organization":"octokit"}""");

        _mockApiClient
            .Setup(c => c.GetOrganizationRepositoriesAsync("octokit", null, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<GitHubRepositoryDto>());

        await _sut.InvokeActionAsync(team.Id, "sync");

        var updated = await _context.Integrations.FindAsync(integration.Id);
        updated!.Status.Should().Be(IntegrationStatus.Connected);
        updated.LastSyncedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task InvokeActionAsync_WithUnsupportedAction_ThrowsUnsupportedAction()
    {
        var (team, _) = await SeedTeamWithGitHubIntegrationAsync("""{"organization":"octokit"}""");

        var act = () => _sut.InvokeActionAsync(team.Id, "delete-everything");

        var ex = await act.Should().ThrowAsync<ModuleConnectorException>();
        ex.Which.Code.Should().Be("GitHub.UnsupportedAction");
    }
}
