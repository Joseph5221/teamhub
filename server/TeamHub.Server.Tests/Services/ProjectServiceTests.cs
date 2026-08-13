// TeamHub.Server.Tests/Services/ProjectServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Modules.Projects;
using TeamHub.Server.Infrastructure.Data;

public class ProjectServiceTests
{
    private readonly AppDbContext _context;
    private readonly ProjectService _sut; // System Under Test

    public ProjectServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _sut = new ProjectService(_context);
    }

    private async Task<(User owner, User member, User outsider, Team team)> SeedTeamAsync()
    {
        var owner = new User { Email = "owner@test.com", Name = "Owner" };
        var member = new User { Email = "member@test.com", Name = "Member" };
        var outsider = new User { Email = "outsider@test.com", Name = "Outsider" };
        var team = new Team { Name = "Growth", Owner = owner, Members = { owner, member } };
        _context.Users.AddRange(owner, member, outsider);
        _context.Teams.Add(team);
        await _context.SaveChangesAsync();
        return (owner, member, outsider, team);
    }

    [Fact]
    public async Task CreateProjectAsync_AsOwner_CreatesProject()
    {
        var (owner, _, _, team) = await SeedTeamAsync();
        var request = new CreateProjectRequest("Dashboard Revamp", "Rebuild the UI", null, null);

        var result = await _sut.CreateProjectAsync(team.Id, owner.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Dashboard Revamp");
        result.Value.TeamId.Should().Be(team.Id);
        result.Value.Status.Should().Be(ProjectStatus.Planned);
    }

    [Fact]
    public async Task CreateProjectAsync_AsNonOwnerMember_ReturnsForbidden()
    {
        var (_, member, _, team) = await SeedTeamAsync();
        var request = new CreateProjectRequest("Sneaky", "", null, null);

        var result = await _sut.CreateProjectAsync(team.Id, member.Id, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.Forbidden");
    }

    [Fact]
    public async Task CreateProjectAsync_WithEmptyName_ReturnsValidationFailure()
    {
        var (owner, _, _, team) = await SeedTeamAsync();
        var request = new CreateProjectRequest("  ", "", null, null);

        var result = await _sut.CreateProjectAsync(team.Id, owner.Id, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.Validation");
    }

    [Fact]
    public async Task CreateProjectAsync_UnknownTeam_ReturnsFailure()
    {
        var request = new CreateProjectRequest("Ghost", "", null, null);

        var result = await _sut.CreateProjectAsync(Guid.NewGuid(), Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.TeamNotFound");
    }

    [Fact]
    public async Task GetProjectsAsync_AsMember_ReturnsTeamProjects()
    {
        var (owner, member, _, team) = await SeedTeamAsync();
        await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Alpha", "", null, null));

        var result = await _sut.GetProjectsAsync(team.Id, member.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().ContainSingle(p => p.Name == "Alpha");
    }

    [Fact]
    public async Task GetProjectsAsync_AsNonMember_ReturnsForbidden()
    {
        var (_, _, outsider, team) = await SeedTeamAsync();

        var result = await _sut.GetProjectsAsync(team.Id, outsider.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.Forbidden");
    }

    [Fact]
    public async Task GetProjectAsync_AsMember_ReturnsProject()
    {
        var (owner, member, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Beta", "", null, null));

        var result = await _sut.GetProjectAsync(team.Id, created.Value!.Id, member.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetProjectAsync_ProjectNotFound_ReturnsFailure()
    {
        var (owner, _, _, team) = await SeedTeamAsync();

        var result = await _sut.GetProjectAsync(team.Id, Guid.NewGuid(), owner.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.NotFound");
    }

    [Fact]
    public async Task UpdateProjectAsync_AsOwner_UpdatesDetails()
    {
        var (owner, _, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Old Name", "Old", null, null));

        var result = await _sut.UpdateProjectAsync(team.Id, created.Value!.Id, owner.Id,
            new UpdateProjectRequest("New Name", "New", ProjectStatus.Active, null, null));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Status.Should().Be(ProjectStatus.Active);
    }

    [Fact]
    public async Task UpdateProjectAsync_AsNonOwnerMember_ReturnsForbidden()
    {
        var (owner, member, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Name", "", null, null));

        var result = await _sut.UpdateProjectAsync(team.Id, created.Value!.Id, member.Id,
            new UpdateProjectRequest("Hacked", "", ProjectStatus.Cancelled, null, null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.Forbidden");
    }

    [Fact]
    public async Task DeleteProjectAsync_AsOwner_RemovesProject()
    {
        var (owner, member, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Doomed", "", null, null));

        var result = await _sut.DeleteProjectAsync(team.Id, created.Value!.Id, owner.Id);

        result.IsSuccess.Should().BeTrue();
        var projects = await _sut.GetProjectsAsync(team.Id, member.Id);
        projects.Value.Should().NotContain(p => p.Id == created.Value.Id);
    }

    [Fact]
    public async Task DeleteProjectAsync_AsNonOwnerMember_ReturnsForbidden()
    {
        var (owner, member, _, team) = await SeedTeamAsync();
        var created = await _sut.CreateProjectAsync(team.Id, owner.Id, new CreateProjectRequest("Safe", "", null, null));

        var result = await _sut.DeleteProjectAsync(team.Id, created.Value!.Id, member.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.Forbidden");
    }

    [Fact]
    public async Task DeleteProjectAsync_ProjectNotFound_ReturnsFailure()
    {
        var (owner, _, _, team) = await SeedTeamAsync();

        var result = await _sut.DeleteProjectAsync(team.Id, Guid.NewGuid(), owner.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Project.NotFound");
    }
}
