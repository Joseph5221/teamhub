using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Teams;

/// <summary>
/// Endpoints for team operations
/// </summary>
public static class TeamEndpoints
{
    public static void MapTeamEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams")
            .WithTags("Teams")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", CreateTeam)
            .WithName("CreateTeam")
            .WithSummary("Create a new team; the caller becomes its owner")
            .Produces<TeamResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

        group.MapGet("/{teamId:guid}", GetTeam)
            .WithName("GetTeam")
            .WithSummary("Get a team's details (members only)")
            .Produces<TeamResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{teamId:guid}", UpdateTeam)
            .WithName("UpdateTeam")
            .WithSummary("Update team settings (owner only)")
            .Produces<TeamResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{teamId:guid}/members", GetMembers)
            .WithName("GetTeamMembers")
            .WithSummary("List a team's members (members only)")
            .Produces<List<TeamMemberResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPost("/{teamId:guid}/members", AddMember)
            .WithName("AddTeamMember")
            .WithSummary("Add an existing user to the team by email (owner only)")
            .Produces<TeamMemberResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);

        group.MapDelete("/{teamId:guid}/members/{memberUserId:guid}", RemoveMember)
            .WithName("RemoveTeamMember")
            .WithSummary("Remove a member from the team (owner only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status409Conflict);
    }

    private static async Task<IResult> CreateTeam(
        [FromBody] CreateTeamRequest request,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.CreateTeamAsync(userId, request);

        return result.IsSuccess
            ? Results.Created($"/api/teams/{result.Value!.Id}", result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetTeam(
        Guid teamId,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.GetTeamAsync(teamId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetMembers(
        Guid teamId,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.GetMembersAsync(teamId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> UpdateTeam(
        Guid teamId,
        [FromBody] UpdateTeamRequest request,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.UpdateTeamAsync(teamId, userId, request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> AddMember(
        Guid teamId,
        [FromBody] AddTeamMemberRequest request,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.AddMemberAsync(teamId, userId, request);

        return result.IsSuccess
            ? Results.Created($"/api/teams/{teamId}/members/{result.Value!.UserId}", result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> RemoveMember(
        Guid teamId,
        Guid memberUserId,
        ClaimsPrincipal user,
        ITeamService teamService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await teamService.RemoveMemberAsync(teamId, userId, memberUserId);

        return result.IsSuccess
            ? Results.NoContent()
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
            "Team.NotFound" or "Team.UserNotFound" or "Team.MemberNotFound" => StatusCodes.Status404NotFound,
            "Team.Forbidden" => StatusCodes.Status403Forbidden,
            "Team.AlreadyMember" or "Team.CannotRemoveOwner" => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
