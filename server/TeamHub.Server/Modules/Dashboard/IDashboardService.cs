using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Dashboard;

/// <summary>
/// Service for dashboard operations
/// </summary>
public interface IDashboardService
{
    /// <summary>
    /// Gets the dashboard data for a user
    /// </summary>
    Task<Result<DashboardResponse>> GetDashboardAsync(Guid userId);
}