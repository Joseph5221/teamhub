using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Modules.Dashboard;

/// <summary>
/// Response model for dashboard data
/// </summary>
public record DashboardResponse(
    UserInfo User,
    List<IntegrationInfo> Integrations,
    DashboardStats Stats
);

/// <summary>
/// User information for dashboard
/// </summary>
public record UserInfo(
    Guid Id,
    string Name,
    string Email,
    UserRole Role
);

/// <summary>
/// Integration information for dashboard
/// </summary>
public record IntegrationInfo(
    Guid Id,
    string Name,
    string Type,
    string Status,
    string Description,
    bool IsEnabled
);

/// <summary>
/// Dashboard statistics
/// </summary>
public record DashboardStats(
    int TotalTeams,
    int TotalProjects,
    int TotalIntegrations,
    int ConnectedIntegrations
);