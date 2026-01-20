using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Infrastructure.Data;
using TeamHub.Server.Infrastructure.Security;

namespace TeamHub.Server.Features.Auth;

/// <summary>
/// Implementation of authentication service
/// NOTE: This is a simplified version for getting started.
/// Password validation is minimal - will be enhanced later.
/// </summary>
public class AuthService : IAuthService
{
    private readonly AppDbContext _context;
    private readonly ITokenService _tokenService;

    public AuthService(AppDbContext context, ITokenService tokenService)
    {
        _context = context;
        _tokenService = tokenService;
    }

    public async Task<Result<AuthResponse>> LoginAsync(LoginRequest request)
    {
        // Find user by email
        var user = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (user == null)
        {
            return Result<AuthResponse>.Failure(
                new Error("Auth.InvalidCredentials", "Invalid email or password"));
        }

        // TODO: For now, we accept any password. Will add proper validation later.
        // This allows easy testing during development

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
        // Check if user already exists
        var existingUser = await _context.Users
            .FirstOrDefaultAsync(u => u.Email == request.Email);

        if (existingUser != null)
        {
            return Result<AuthResponse>.Failure(
                new Error("Auth.DuplicateEmail", "A user with this email already exists"));
        }

        // Create new user
        // TODO: Hash password properly later
        var user = new User
        {
            Email = request.Email,
            Name = request.Name,
            PasswordHash = request.Password, // Will be hashed later
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