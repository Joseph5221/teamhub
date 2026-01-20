using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Features.Dashboard;

/// <summary>
/// Implementation of dashboard service
/// </summary>
public class DashboardService : IDashboardService
{
    private readonly AppDbContext _context;

    public DashboardService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<DashboardResponse>> GetDashboardAsync(Guid userId)
    {
        // Get user with related data
        var user = await _context.Users
            .Include(u => u.Integrations)
            .Include(u => u.Teams)
            .FirstOrDefaultAsync(u => u.Id == userId);

        if (user == null)
        {
            return Result<DashboardResponse>.Failure(
                Error.NotFound("User", userId));
        }

        // Get user's projects through teams
        var projectCount = await _context.Projects
            .Where(p => user.Teams.Select(t => t.Id).Contains(p.TeamId))
            .CountAsync();

        // Map integrations
        var integrations = user.Integrations
            .OrderBy(i => i.Status) // TODO items first
            .Select(i => new IntegrationInfo(
                i.Id,
                i.Name,
                i.Type,
                i.Status,
                i.Description,
                i.IsEnabled
            ))
            .ToList();

        // Calculate stats
        var stats = new DashboardStats(
            TotalTeams: user.Teams.Count,
            TotalProjects: projectCount,
            TotalIntegrations: integrations.Count,
            ConnectedIntegrations: integrations.Count(i => i.Status == "Connected")
        );

        var userInfo = new UserInfo(
            user.Id,
            user.Name,
            user.Email,
            user.Role
        );

        var response = new DashboardResponse(
            userInfo,
            integrations,
            stats
        );

        return Result<DashboardResponse>.Success(response);
    }
}