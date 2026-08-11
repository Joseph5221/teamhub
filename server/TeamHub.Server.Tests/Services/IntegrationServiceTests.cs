// TeamHub.Server.Tests/Services/IntegrationServiceTests.cs
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Modules.Integrations;
using TeamHub.Server.Infrastructure.Data;

public class IntegrationServiceTests
{
    private readonly AppDbContext _context;
    private readonly Mock<IModuleConnector> _mockConnector;
    private readonly IntegrationService _sut;

    public IntegrationServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _mockConnector = new Mock<IModuleConnector>();
        _mockConnector.SetupGet(c => c.Type).Returns(IntegrationType.VersionControl);

        _sut = new IntegrationService(_context, new[] { _mockConnector.Object });
    }

    private async Task<(User owner, User member, Team team)> SeedTeamAsync()
    {
        var owner = new User { Email = "owner@test.com", Name = "Owner" };
        var member = new User { Email = "member@test.com", Name = "Member" };
        var team = new Team { Name = "Growth", Owner = owner, Members = { owner, member } };
        _context.Users.AddRange(owner, member);
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        return (owner, member, team);
    }

    [Fact]
    public async Task CreateIntegrationAsync_AsOwner_CreatesIntegration()
    {
        var (owner, _, team) = await SeedTeamAsync();
        var request = new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", new() { ["organization"] = "octokit" });

        var result = await _sut.CreateIntegrationAsync(team.Id, owner.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("GitHub");
        result.Value.IsConfigured.Should().BeTrue();
    }

    [Fact]
    public async Task CreateIntegrationAsync_AsNonOwnerMember_ReturnsForbidden()
    {
        var (_, member, team) = await SeedTeamAsync();
        var request = new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null);

        var result = await _sut.CreateIntegrationAsync(team.Id, member.Id, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Integration.Forbidden");
    }

    [Fact]
    public async Task CreateIntegrationAsync_WithEmptyName_ReturnsValidationFailure()
    {
        var (owner, _, team) = await SeedTeamAsync();
        var request = new CreateIntegrationRequest("  ", IntegrationType.VersionControl, "Repos", null);

        var result = await _sut.CreateIntegrationAsync(team.Id, owner.Id, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Integration.Validation");
    }

    [Fact]
    public async Task GetIntegrationsAsync_AsMember_ReturnsTeamIntegrations()
    {
        var (owner, member, team) = await SeedTeamAsync();
        await _sut.CreateIntegrationAsync(team.Id, owner.Id, new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null));

        var result = await _sut.GetIntegrationsAsync(team.Id, member.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(i => i.Name == "GitHub");
    }

    [Fact]
    public async Task GetIntegrationsAsync_AsNonMember_ReturnsForbidden()
    {
        var (_, _, team) = await SeedTeamAsync();
        var outsider = new User { Email = "outsider@test.com", Name = "Outsider" };
        _context.Users.Add(outsider);
        await _context.SaveChangesAsync();

        var result = await _sut.GetIntegrationsAsync(team.Id, outsider.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Integration.Forbidden");
    }

    [Fact]
    public async Task DeleteIntegrationAsync_AsOwner_RemovesIntegration()
    {
        var (owner, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateIntegrationAsync(team.Id, owner.Id, new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null));

        var result = await _sut.DeleteIntegrationAsync(team.Id, created.Value!.Id, owner.Id);

        result.IsSuccess.Should().BeTrue();
        (await _context.Integrations.FindAsync(created.Value.Id)).Should().BeNull();
    }

    [Fact]
    public async Task GetIntegrationDataAsync_WithRegisteredConnector_ReturnsConnectorData()
    {
        var (owner, member, team) = await SeedTeamAsync();
        var created = await _sut.CreateIntegrationAsync(team.Id, owner.Id, new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null));

        var moduleData = new ModuleData(DateTime.UtcNow, new List<ModuleDataItem> { new("1", "repo", null, null, null) });
        _mockConnector
            .Setup(c => c.GetDataAsync(team.Id, null))
            .ReturnsAsync(moduleData);

        var result = await _sut.GetIntegrationDataAsync(team.Id, created.Value!.Id, member.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Items.Should().ContainSingle(i => i.Title == "repo");
    }

    [Fact]
    public async Task GetIntegrationDataAsync_WithNoConnectorForType_ReturnsNotSupported()
    {
        var (owner, member, team) = await SeedTeamAsync();
        var created = await _sut.CreateIntegrationAsync(
            team.Id, owner.Id, new CreateIntegrationRequest("Jira", IntegrationType.ProjectManagement, "Issues", null));

        var result = await _sut.GetIntegrationDataAsync(team.Id, created.Value!.Id, member.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Integration.NotSupported");
    }

    [Fact]
    public async Task GetIntegrationDataAsync_WhenConnectorThrows_ReturnsFailureAndMarksIntegrationFailed()
    {
        var (owner, member, team) = await SeedTeamAsync();
        var created = await _sut.CreateIntegrationAsync(team.Id, owner.Id, new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null));

        _mockConnector
            .Setup(c => c.GetDataAsync(team.Id, null))
            .ThrowsAsync(new ModuleConnectorException("GitHub.NotConfigured", "not configured"));

        var result = await _sut.GetIntegrationDataAsync(team.Id, created.Value!.Id, member.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("GitHub.NotConfigured");

        var integration = await _context.Integrations.FindAsync(created.Value.Id);
        integration!.Status.Should().Be(IntegrationStatus.Failed);
    }

    [Fact]
    public async Task InvokeIntegrationActionAsync_AsMember_InvokesConnectorAction()
    {
        var (owner, member, team) = await SeedTeamAsync();
        var created = await _sut.CreateIntegrationAsync(team.Id, owner.Id, new CreateIntegrationRequest("GitHub", IntegrationType.VersionControl, "Repos", null));

        var result = await _sut.InvokeIntegrationActionAsync(team.Id, created.Value!.Id, member.Id, new InvokeIntegrationActionRequest("sync"));

        result.IsSuccess.Should().BeTrue();
        _mockConnector.Verify(c => c.InvokeActionAsync(team.Id, "sync"), Times.Once);
    }
}
