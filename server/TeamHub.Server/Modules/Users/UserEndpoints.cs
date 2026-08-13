using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Users;

/// <summary>
/// Endpoints for user profile data and role assignment
/// </summary>
public static class UserEndpoints
{
    public static void MapUserEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/users")
            .WithTags("Users")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapGet("/me", GetOwnProfile)
            .WithName("GetOwnProfile")
            .WithSummary("Get the caller's own profile")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK);

        group.MapPut("/me", UpdateOwnProfile)
            .WithName("UpdateOwnProfile")
            .WithSummary("Update the caller's own profile (display name, avatar)")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{userId:guid}", GetProfile)
            .WithName("GetUserProfile")
            .WithSummary("Get a user's public profile")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{userId:guid}/role", UpdateRole)
            .WithName("UpdateUserRole")
            .WithSummary("Assign a user's system-wide role (admin only)")
            .Produces<UserProfileResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetOwnProfile(
        ClaimsPrincipal user,
        IUserService userService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await userService.GetProfileAsync(userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> UpdateOwnProfile(
        [FromBody] UpdateProfileRequest request,
        ClaimsPrincipal user,
        IUserService userService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await userService.UpdateProfileAsync(userId, request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetProfile(
        Guid userId,
        IUserService userService)
    {
        var result = await userService.GetProfileAsync(userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> UpdateRole(
        Guid userId,
        [FromBody] UpdateUserRoleRequest request,
        ClaimsPrincipal user,
        IUserService userService)
    {
        if (!TryGetUserId(user, out var requestingUserId))
        {
            return Unauthorized();
        }

        var result = await userService.UpdateRoleAsync(userId, requestingUserId, request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static bool TryGetUserId(ClaimsPrincipal user, out Guid userId)
    {
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value
                       ?? user.FindFirst("sub")?.Value;

        return Guid.TryParse(userIdClaim, out userId);
    }

    private static IResult Unauthorized() =>
        Results.Problem(
            statusCode: StatusCodes.Status401Unauthorized,
            title: "Unauthorized",
            detail: "User ID not found in token"
        );

    private static IResult ToProblem(Error error)
    {
        var statusCode = error.Code switch
        {
            "User.NotFound" => StatusCodes.Status404NotFound,
            "User.Forbidden" => StatusCodes.Status403Forbidden,
            "User.Validation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
