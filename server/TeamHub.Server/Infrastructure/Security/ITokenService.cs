using TeamHub.Server.Domain.Entities;

namespace TeamHub.Server.Infrastructure.Security;

/// <summary>
/// Service for generating and validating JWT tokens
/// </summary>
public interface ITokenService
{
    /// <summary>
    /// Generates a JWT token for the given user
    /// </summary>
    string GenerateToken(User user);

    /// <summary>
    /// Validates a JWT token and returns the user ID
    /// </summary>
    Guid? ValidateToken(string token);
}