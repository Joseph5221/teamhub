using Microsoft.AspNetCore.Mvc;

namespace TeamHub.Server.Modules.Auth;

/// <summary>
/// Endpoints for authentication operations
/// </summary>
public static class AuthEndpoints
{
    public static void MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/auth")
            .WithTags("Authentication")
            .WithOpenApi();

        group.MapPost("/login", Login)
            .WithName("Login")
            .WithSummary("Authenticate a user and receive a JWT token")
            .Produces<AuthResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);

        group.MapPost("/register", Register)
            .WithName("Register")
            .WithSummary("Register a new user account")
            .Produces<AuthResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);
    }

    private static async Task<IResult> Login(
        [FromBody] LoginRequest request,
        IAuthService authService)
    {
        var result = await authService.LoginAsync(request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: result.Error!.Code,
                detail: result.Error.Message
            );
    }

    private static async Task<IResult> Register(
        [FromBody] RegisterRequest request,
        IAuthService authService)
    {
        var result = await authService.RegisterAsync(request);

        return result.IsSuccess
            ? Results.Created($"/api/users/{result.Value!.UserId}", result.Value)
            : Results.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: result.Error!.Code,
                detail: result.Error.Message
            );
    }
}