namespace BlazorApp.Models;

public record LoginRequest(string Email, string Password);

public record RegisterRequest(string Email, string Name, string Password);

public record AuthResponse(Guid UserId, string Email, string Name, string Token, string Role);
