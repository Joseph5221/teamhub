using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// Endpoints for a team's configured integrations, nested under
/// <c>/api/teams/{teamId}/integrations</c> to match <c>TeamEndpoints</c>'
/// nesting of members under a team.
/// </summary>
public static class IntegrationEndpoints
{
    public static void MapIntegrationEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/teams/{teamId:guid}/integrations")
            .WithTags("Integrations")
            .WithOpenApi()
            .RequireAuthorization();

        group.MapPost("/", CreateIntegration)
            .WithName("CreateIntegration")
            .WithSummary("Configure a new integration for a team (owner only)")
            .Produces<IntegrationResponse>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/", GetIntegrations)
            .WithName("GetIntegrations")
            .WithSummary("List a team's configured integrations (members only)")
            .Produces<List<IntegrationResponse>>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden);

        group.MapGet("/{integrationId:guid}", GetIntegration)
            .WithName("GetIntegration")
            .WithSummary("Get a single integration's details (members only)")
            .Produces<IntegrationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapPut("/{integrationId:guid}", UpdateIntegration)
            .WithName("UpdateIntegration")
            .WithSummary("Update an integration's settings/configuration (owner only)")
            .Produces<IntegrationResponse>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapDelete("/{integrationId:guid}", DeleteIntegration)
            .WithName("DeleteIntegration")
            .WithSummary("Remove an integration from a team (owner only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound);

        group.MapGet("/{integrationId:guid}/data", GetIntegrationData)
            .WithName("GetIntegrationData")
            .WithSummary("Fetch normalized data from the integration's connector (members only)")
            .Produces<ModuleData>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status501NotImplemented)
            .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);

        group.MapPost("/{integrationId:guid}/actions", InvokeIntegrationAction)
            .WithName("InvokeIntegrationAction")
            .WithSummary("Invoke a connector-specific action, e.g. \"sync\" (members only)")
            .Produces(StatusCodes.Status204NoContent)
            .Produces<ProblemDetails>(StatusCodes.Status403Forbidden)
            .Produces<ProblemDetails>(StatusCodes.Status404NotFound)
            .Produces<ProblemDetails>(StatusCodes.Status501NotImplemented)
            .Produces<ProblemDetails>(StatusCodes.Status502BadGateway);
    }

    private static async Task<IResult> CreateIntegration(
        Guid teamId,
        [FromBody] CreateIntegrationRequest request,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.CreateIntegrationAsync(teamId, userId, request);

        return result.IsSuccess
            ? Results.Created($"/api/teams/{teamId}/integrations/{result.Value!.Id}", result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetIntegrations(
        Guid teamId,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.GetIntegrationsAsync(teamId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetIntegration(
        Guid teamId,
        Guid integrationId,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.GetIntegrationAsync(teamId, integrationId, userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> UpdateIntegration(
        Guid teamId,
        Guid integrationId,
        [FromBody] UpdateIntegrationRequest request,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.UpdateIntegrationAsync(teamId, integrationId, userId, request);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> DeleteIntegration(
        Guid teamId,
        Guid integrationId,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.DeleteIntegrationAsync(teamId, integrationId, userId);

        return result.IsSuccess
            ? Results.NoContent()
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> GetIntegrationData(
        Guid teamId,
        Guid integrationId,
        DateTime? since,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.GetIntegrationDataAsync(teamId, integrationId, userId, since);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : ToProblem(result.Error!);
    }

    private static async Task<IResult> InvokeIntegrationAction(
        Guid teamId,
        Guid integrationId,
        [FromBody] InvokeIntegrationActionRequest request,
        ClaimsPrincipal user,
        IIntegrationService integrationService)
    {
        if (!TryGetUserId(user, out var userId))
        {
            return Unauthorized();
        }

        var result = await integrationService.InvokeIntegrationActionAsync(teamId, integrationId, userId, request);

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
            "Integration.TeamNotFound" or "Integration.NotFound" => StatusCodes.Status404NotFound,
            "Integration.Forbidden" => StatusCodes.Status403Forbidden,
            "Integration.Validation" => StatusCodes.Status400BadRequest,
            "Integration.NotSupported" => StatusCodes.Status501NotImplemented,
            "GitHub.OrganizationNotFound" => StatusCodes.Status404NotFound,
            "GitHub.Unauthorized" => StatusCodes.Status401Unauthorized,
            "GitHub.NotConfigured" or "GitHub.UnsupportedAction" => StatusCodes.Status400BadRequest,
            "GitHub.ApiError" => StatusCodes.Status502BadGateway,
            _ => StatusCodes.Status400BadRequest
        };

        return Results.Problem(statusCode: statusCode, title: error.Code, detail: error.Message);
    }
}
