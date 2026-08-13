// TeamHub.Server.Tests/Services/UserServiceTests.cs
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Modules.Users;
using TeamHub.Server.Infrastructure.Data;

public class UserServiceTests
{
    private readonly AppDbContext _context;
    private readonly UserService _sut; // System Under Test

    public UserServiceTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        _sut = new UserService(_context);
    }

    private async Task<User> CreateUserAsync(string email, string name = "Test User", UserRole role = UserRole.Member)
    {
        var user = new User { Email = email, Name = name, Role = role };
        _context.Users.Add(user);
        await _context.SaveChangesAsync();
        return user;
    }

    [Fact]
    public async Task GetProfileAsync_ExistingUser_ReturnsProfile()
    {
        var user = await CreateUserAsync("alice@test.com", "Alice");

        var result = await _sut.GetProfileAsync(user.Id);

        result.IsSuccess.Should().BeTrue();
        result.Value!.Id.Should().Be(user.Id);
        result.Value.Name.Should().Be("Alice");
        result.Value.Role.Should().Be(UserRole.Member);
    }

    [Fact]
    public async Task GetProfileAsync_UnknownUser_ReturnsFailure()
    {
        var result = await _sut.GetProfileAsync(Guid.NewGuid());

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithValidRequest_UpdatesNameAndAvatar()
    {
        var user = await CreateUserAsync("bob@test.com", "Bob");

        var result = await _sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("Bob Builder", "https://example.com/avatar.png"));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Name.Should().Be("Bob Builder");
        result.Value.AvatarUrl.Should().Be("https://example.com/avatar.png");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithEmptyName_ReturnsFailure()
    {
        var user = await CreateUserAsync("carol@test.com");

        var result = await _sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("  ", null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.Validation");
    }

    [Fact]
    public async Task UpdateProfileAsync_WithInvalidAvatarUrl_ReturnsFailure()
    {
        var user = await CreateUserAsync("dave@test.com");

        var result = await _sut.UpdateProfileAsync(user.Id, new UpdateProfileRequest("Dave", "not-a-url"));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.Validation");
    }

    [Fact]
    public async Task UpdateProfileAsync_UnknownUser_ReturnsFailure()
    {
        var result = await _sut.UpdateProfileAsync(Guid.NewGuid(), new UpdateProfileRequest("Nobody", null));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.NotFound");
    }

    [Fact]
    public async Task UpdateRoleAsync_AsAdmin_UpdatesTargetRole()
    {
        var admin = await CreateUserAsync("admin@test.com", role: UserRole.Admin);
        var target = await CreateUserAsync("target@test.com");

        var result = await _sut.UpdateRoleAsync(target.Id, admin.Id, new UpdateUserRoleRequest(UserRole.Admin));

        result.IsSuccess.Should().BeTrue();
        result.Value!.Role.Should().Be(UserRole.Admin);
    }

    [Fact]
    public async Task UpdateRoleAsync_AsNonAdmin_ReturnsForbidden()
    {
        var member = await CreateUserAsync("member@test.com");
        var target = await CreateUserAsync("target2@test.com");

        var result = await _sut.UpdateRoleAsync(target.Id, member.Id, new UpdateUserRoleRequest(UserRole.Admin));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.Forbidden");
    }

    [Fact]
    public async Task UpdateRoleAsync_UnknownTargetUser_ReturnsFailure()
    {
        var admin = await CreateUserAsync("admin2@test.com", role: UserRole.Admin);

        var result = await _sut.UpdateRoleAsync(Guid.NewGuid(), admin.Id, new UpdateUserRoleRequest(UserRole.Admin));

        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("User.NotFound");
    }
}
