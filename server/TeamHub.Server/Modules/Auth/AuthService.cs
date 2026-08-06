using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

namespace TeamHub.Server.Modules.Auth;

/// <summary>
/// Implementation of authentication service
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;
    private readonly IPasswordHasher _passwordHasher;
    private readonly IPasswordValidator _passwordValidator;

    public AuthService(
        AppDbContext context,
        ITokenService tokenService,
        IPasswordHasher passwordHasher,
        IPasswordValidator passwordValidator)
    {
        _context = context;
        _tokenService = tokenService;
        _passwordHasher = passwordHasher;
        _passwordValidator = passwordValidator;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null || !_passwordHasher.VerifyPassword(user.PasswordHash, request.Password))
        {
            return Result<AuthResponse>.Failure(
                new Error("Auth.InvalidCredentials", "Invalid email or password"));
        }

        if (!user.IsActive)
        {
            return Result<AuthResponse>.Failure(
                new Error("Auth.Inactive", "User account is not active"));
        }

        // Generate JWT token
        var token = _tokenService.GenerateToken(user);

        var response = new AuthResponse(
            user.Id,
            user.Email,
            user.Name,
            token,
            user.Role
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request)
    {
        var passwordValidation = _passwordValidator.Validate(request.Password);
        if (passwordValidation.IsFailure)
        {
            return Result<AuthResponse>.Failure(passwordValidation.Error!);
        }

        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure(
                new Error("Auth.DuplicateEmail", "A user with this email already exists"));
        }

        // Create new user
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = _passwordHasher.HashPassword(request.Password),
            Role = "User"
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Generate token
        var token = _tokenService.GenerateToken(user);

        var response = new AuthResponse(
            user.Id,
            user.Email,
            user.Name,
            token,
            user.Role
        );

        return Result<AuthResponse>.Success(response);
    }

    public async Task<Result<Guid>> ValidateTokenAsync(string token)
    {
        var userId = _tokenService.ValidateToken(token);
        
        if (userId == null)
        {
            return Result<Guid>.Failure(
                new Error("Auth.InvalidToken", "Invalid or expired token"));
        }

        // Verify user still exists and is active
        var user = await _context.Users.FindAsync(userId.Value);
        
        if (user == null || !user.IsActive)
        {
            return Result<Guid>.Failure(
                new Error("Auth.InvalidUser", "User not found or inactive"));
        }

        return Result<Guid>.Success(userId.Value);
    }
}