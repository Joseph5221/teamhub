// TeamHub.Server.Tests/Services/AuthServiceTests.cs
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Modules.Auth;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _mockTokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;
    private readonly AppDbContext _context;
    private readonly AuthService _sut; // System Under Test

    public AuthServiceTests()
    {
        // Arrange - Set up in-memory database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);

        // Arrange - Set up mocks
        _mockTokenService = new Mock<ITokenService>();
        _mockTokenService
            .Setup(x => x.GenerateToken(It.IsAny<User>()))
            .Returns("test-token");
        _passwordHasher = new PasswordHasher();
        _passwordValidator = new PasswordValidator();

        // Arrange - Create system under test
        _sut = new AuthService(_context, _mockTokenService.Object, _passwordHasher, _passwordValidator);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var user = new User
        {
            Email = "test@test.com",
            Name = "Test User",
            PasswordHash = _passwordHasher.HashPassword("password"),
            IsActive = true
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("test@test.com", "password");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBeNull();
        result.Value!.Email.Should().Be("test@test.com");
        result.Value.Token.Should().Be("test-token");
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ReturnsFailure()
    {
        // Arrange
        var user = new User
        {
            Email = "test2@test.com",
            Name = "Test User",
            PasswordHash = _passwordHasher.HashPassword("password"),
            IsActive = true
        };
        await _context.Users.AddAsync(user);
        await _context.SaveChangesAsync();

        var request = new LoginRequest("test2@test.com", "wrong-password");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task LoginAsync_WithInvalidEmail_ReturnsFailure()
    {
        // Arrange
        var request = new LoginRequest("nonexistent@test.com", "password");

        // Act
        var result = await _sut.LoginAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.InvalidCredentials");
    }

    [Fact]
    public async Task RegisterAsync_WithValidPassword_ReturnsSuccessResult()
    {
        // Arrange
        var request = new RegisterRequest("new@test.com", "New User", "password123");

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value!.Email.Should().Be("new@test.com");

        var storedUser = await _context.Users.FirstAsync(u => u.Email == "new@test.com");
        storedUser.PasswordHash.Should().NotBe("password123");
        _passwordHasher.VerifyPassword(storedUser.PasswordHash, "password123").Should().BeTrue();
    }

    [Theory]
    [InlineData("short1")]      // too short
    [InlineData("alllettersnodigits")] // no digit
    [InlineData("12345678")]    // no letter
    public async Task RegisterAsync_WithWeakPassword_ReturnsFailure(string weakPassword)
    {
        // Arrange
        var request = new RegisterRequest("weak@test.com", "Weak Password User", weakPassword);

        // Act
        var result = await _sut.RegisterAsync(request);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error!.Code.Should().Be("Auth.WeakPassword");
        (await _context.Users.AnyAsync(u => u.Email == "weak@test.com")).Should().BeFalse();
    }
}