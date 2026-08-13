using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Projects;

/// <summary>
/// Endpoints for a team's projects, nested under
/// <c>/api/teams/{teamId}/projects</c> to match <c>TeamEndpoints</c>'/
/// <c>IntegrationEndpoints</c>' nesting under a team.
/// </summary>
public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams/{teamId:guid}/projects")
            .WithTags("Projects")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", CreateProject)
            .WithName("CreateProject")
            .WithSummary("Create a new project for a team (owner only)")
            .Produces<ProjectResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/", GetProjects)
            .WithName("GetProjects")
            .WithSummary("List a team's projects (members only)")
            .Produces<List<ProjectResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/{projectId:guid}", GetProject)
            .WithName("GetProject")
            .WithSummary("Get a single project's details (members only)")
            .Produces<ProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{projectId:guid}", UpdateProject)
            .WithName("UpdateProject")
            .WithSummary("Update a project's details (owner only)")
            .Produces<ProjectResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{projectId:guid}", DeleteProject)
            .WithName("DeleteProject")
            .WithSummary("Remove a project from a team (owner only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> CreateProject(
        Guid teamId,
        [FromBody] CreateProjectRequest request,
        ClaimsPrincipal user,
        IProjectService projectService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await projectService.CreateProjectAsync(teamId, userId, request);

        return result.IsSuccess
            ? Results.Created($"/api/teams/{teamId}/projects/{result.Value!.Id}", result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetProjects(
        Guid teamId,
        ClaimsPrincipal user,
        IProjectService projectService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await projectService.GetProjectsAsync(teamId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetProject(
        Guid teamId,
        Guid projectId,
        ClaimsPrincipal user,
        IProjectService projectService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await projectService.GetProjectAsync(teamId, projectId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> UpdateProject(
        Guid teamId,
        Guid projectId,
        [FromBody] UpdateProjectRequest request,
        ClaimsPrincipal user,
        IProjectService projectService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await projectService.UpdateProjectAsync(teamId, projectId, userId, request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> DeleteProject(
        Guid teamId,
        Guid projectId,
        ClaimsPrincipal user,
        IProjectService projectService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await projectService.DeleteProjectAsync(teamId, projectId, userId);

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
            "Project.TeamNotFound" or "Project.NotFound" => StatusCodes.Status404NotFound,
            "Project.Forbidden" => StatusCodes.Status403Forbidden,
            "Project.Validation" => StatusCodes.Status400BadRequest,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
