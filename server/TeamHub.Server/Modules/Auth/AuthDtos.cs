namespace TeamHub.Server.Modules.Auth;

/// <summary>
/// Request model for user login
/// </summary>
public record LoginRequest(string Email, string Password);

/// <summary>
/// Request model for user registration
/// </summary>
public record RegisterRequest(string Email, string Name, string Password);

/// <summary>
/// Response model for successful authentication
/// </summary>
public record AuthResponse(
    Guid UserId,
    string Email,
    string Name,
    string Token,
    string Role
);

/// <summary>
/// Response model for validation errors
/// </summary>
public record ValidationErrorResponse(Dictionary<string, string[]> Errors);