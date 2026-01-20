using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace TeamHub.Server.Features.Dashboard;

/// <summary>
/// Endpoints for dashboard operations
/// </summary>
public static class DashboardEndpoints
{
    public static void MapDashboardEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dashboard")
            .WithTags("Dashboard")
            .WithOpenApi()
            .RequireAuthorization(); // Requires JWT authentication

        group.MapGet("/", GetDashboard)
            .WithName("GetDashboard")
            .WithSummary("Get dashboard data for the authenticated user")
            .Produces<DashboardResponse>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetDashboard(
        ClaimsPrincipal user,
        IDashboardService dashboardService)
    {
        // Get user ID from JWT claims
        var userIdClaim = user.FindFirst(ClaimTypes.NameIdentifier)?.Value 
                       ?? user.FindFirst("sub")?.Value;

        if (userIdClaim == null || !Guid.TryParse(userIdClaim, out var userId))
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: "User ID not found in token"
            );
        }

        var result = await dashboardService.GetDashboardAsync(userId);

        return result.IsSuccess
            ? Results.Ok(result.Value)
            : Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: result.Error!.Code,
                detail: result.Error.Message
            );
    }
}