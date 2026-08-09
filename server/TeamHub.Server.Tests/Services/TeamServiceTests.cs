// TeamHub.Server.Tests/Services/TeamServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Modules.Teams;
using TeamHub.Server.Infrastructure.Data;

public class TeamServiceTests
{
    private readonly AppDbContext _context;
    private readonly TeamService _sut; // System Under Test

    public TeamServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _sut = new TeamService(_context);
    }

    private async Task<User> CreateUserAsync(string email, string name = "Test User")
    {
        var user = new User { Email = email, Name = name };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task CreateTeamAsync_WithValidRequest_CreatesTeamWithCallerAsOwnerAndMember()
    {
        var owner = await CreateUserAsync("owner@test.com");
        var request = new CreateTeamRequest("Platform", "Platform team");

        var result = await _sut.CreateTeamAsync(owner.Id, request);

        result.IsSuccess.Should().BeTrue();
        result.Value!.OwnerId.Should().Be(owner.Id);
        result.Value.Name.Should().Be("Platform");
        result.Value.Members.Should().ContainSingle(m => m.UserId == owner.Id && m.Role == "Owner");
    }

    [Fact]
    public async Task CreateTeamAsync_WithEmptyName_ReturnsFailure()
    {
        var owner = await CreateUserAsync("owner2@test.com");
        var request = new CreateTeamRequest("  ", "Description");

        var result = await _sut.CreateTeamAsync(owner.Id, request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.Validation");
    }

    [Fact]
    public async Task CreateTeamAsync_WithUnknownOwner_ReturnsFailure()
    {
        var request = new CreateTeamRequest("Platform", "Description");

        var result = await _sut.CreateTeamAsync(Guid.NewGuid(), request);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.UserNotFound");
    }

    [Fact]
    public async Task GetTeamAsync_AsMember_ReturnsTeam()
    {
        var owner = await CreateUserAsync("owner3@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Growth", ""));

        var result = await _sut.GetTeamAsync(created.Value!.Id, owner.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(created.Value.Id);
    }

    [Fact]
    public async Task GetTeamAsync_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateUserAsync("owner4@test.com");
        var outsider = await CreateUserAsync("outsider@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Growth", ""));

        var result = await _sut.GetTeamAsync(created.Value!.Id, outsider.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.Forbidden");
    }

    [Fact]
    public async Task GetTeamAsync_TeamNotFound_ReturnsFailure()
    {
        var owner = await CreateUserAsync("owner5@test.com");

        var result = await _sut.GetTeamAsync(Guid.NewGuid(), owner.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.NotFound");
    }

    [Fact]
    public async Task UpdateTeamAsync_AsOwner_UpdatesSettings()
    {
        var owner = await CreateUserAsync("owner6@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Old Name", "Old Description"));

        var result = await _sut.UpdateTeamAsync(created.Value!.Id, owner.Id, new UpdateTeamRequest("New Name", "New Description"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("New Name");
        result.Value.Description.Should().Be("New Description");
    }

    [Fact]
    public async Task UpdateTeamAsync_AsNonOwnerMember_ReturnsForbidden()
    {
        var owner = await CreateUserAsync("owner7@test.com");
        var member = await CreateUserAsync("member7@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));
        await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(member.Email));

        var result = await _sut.UpdateTeamAsync(created.Value.Id, member.Id, new UpdateTeamRequest("Hacked", ""));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.Forbidden");
    }

    [Fact]
    public async Task AddMemberAsync_AsOwner_AddsExistingUserByEmail()
    {
        var owner = await CreateUserAsync("owner8@test.com");
        var newMember = await CreateUserAsync("newmember8@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));

        var result = await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(newMember.Email));

        result.IsSuccess.Should().BeTrue();
        result.Value!.UserId.Should().Be(newMember.Id);
        result.Value.Role.Should().Be("Member");

        var members = await _sut.GetMembersAsync(created.Value.Id, owner.Id);
        members.Value.Should().Contain(m => m.UserId == newMember.Id);
    }

    [Fact]
    public async Task AddMemberAsync_AsNonOwner_ReturnsForbidden()
    {
        var owner = await CreateUserAsync("owner9@test.com");
        var member = await CreateUserAsync("member9@test.com");
        var outsider = await CreateUserAsync("outsider9@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));
        await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(member.Email));

        var result = await _sut.AddMemberAsync(created.Value.Id, member.Id, new AddTeamMemberRequest(outsider.Email));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.Forbidden");
    }

    [Fact]
    public async Task AddMemberAsync_WithUnknownEmail_ReturnsFailure()
    {
        var owner = await CreateUserAsync("owner10@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));

        var result = await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest("nobody@test.com"));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.UserNotFound");
    }

    [Fact]
    public async Task AddMemberAsync_AlreadyMember_ReturnsConflict()
    {
        var owner = await CreateUserAsync("owner11@test.com");
        var member = await CreateUserAsync("member11@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));
        await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(member.Email));

        var result = await _sut.AddMemberAsync(created.Value.Id, owner.Id, new AddTeamMemberRequest(member.Email));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.AlreadyMember");
    }

    [Fact]
    public async Task RemoveMemberAsync_AsOwner_RemovesMember()
    {
        var owner = await CreateUserAsync("owner12@test.com");
        var member = await CreateUserAsync("member12@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));
        await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(member.Email));

        var result = await _sut.RemoveMemberAsync(created.Value.Id, owner.Id, member.Id);

        result.IsSuccess.Should().BeTrue();
        var members = await _sut.GetMembersAsync(created.Value.Id, owner.Id);
        members.Value.Should().NotContain(m => m.UserId == member.Id);
    }

    [Fact]
    public async Task RemoveMemberAsync_AsNonOwner_ReturnsForbidden()
    {
        var owner = await CreateUserAsync("owner13@test.com");
        var member1 = await CreateUserAsync("member13a@test.com");
        var member2 = await CreateUserAsync("member13b@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));
        await _sut.AddMemberAsync(created.Value!.Id, owner.Id, new AddTeamMemberRequest(member1.Email));
        await _sut.AddMemberAsync(created.Value.Id, owner.Id, new AddTeamMemberRequest(member2.Email));

        var result = await _sut.RemoveMemberAsync(created.Value.Id, member1.Id, member2.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.Forbidden");
    }

    [Fact]
    public async Task RemoveMemberAsync_OwnerCannotRemoveThemselves()
    {
        var owner = await CreateUserAsync("owner14@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));

        var result = await _sut.RemoveMemberAsync(created.Value!.Id, owner.Id, owner.Id);

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.CannotRemoveOwner");
    }

    [Fact]
    public async Task RemoveMemberAsync_MemberNotFound_ReturnsFailure()
    {
        var owner = await CreateUserAsync("owner15@test.com");
        var created = await _sut.CreateTeamAsync(owner.Id, new CreateTeamRequest("Team", ""));

        var result = await _sut.RemoveMemberAsync(created.Value!.Id, owner.Id, Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Team.MemberNotFound");
    }
}
