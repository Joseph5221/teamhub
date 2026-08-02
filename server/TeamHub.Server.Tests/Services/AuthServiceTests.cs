// TeamHub.Server.Tests/Services/AuthServiceTests.cs
using Moq;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Features.Auth;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

public class AuthServiceTests
{
    private readonly Mock<ITokenService> _mockTokenService;
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
        
        // Arrange - Create system under test
        _sut = new AuthService(_context, _mockTokenService.Object);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_ReturnsSuccessResult()
    {
        // Arrange
        var user = new User 
        { 
            Email = "test@test.com", 
            Name = "Test User",
            PasswordHash = "password",
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
}