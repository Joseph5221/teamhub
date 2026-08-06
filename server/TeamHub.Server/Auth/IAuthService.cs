using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Features.Auth;

/// <summary>
/// Service for handling authentication operations
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and returns a JWT token
    /// </summary>
    Task<Result<AuthResponse>> LoginAsync(LoginRequest request);

    /// <summary>
    /// Registers a new user
    /// </summary>
    Task<Result<AuthResponse>> RegisterAsync(RegisterRequest request);

    /// <summary>
    /// Validates a JWT token
    /// </summary>
    Task<Result<Guid>> ValidateTokenAsync(string token);
}