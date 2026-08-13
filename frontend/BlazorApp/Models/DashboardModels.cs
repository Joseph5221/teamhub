namespace BlazorApp.Models;

public record DashboardResponse(UserInfo User, List<IntegrationInfo> Integrations, DashboardStats Stats);

public record UserInfo(Guid Id, string Name, string Email, string Role);

public record IntegrationInfo(Guid Id, string Name, string Type, string Status, string Description, bool IsEnabled);

public record DashboardStats(int TotalTeams, int TotalProjects, int TotalIntegrations, int ConnectedIntegrations);
