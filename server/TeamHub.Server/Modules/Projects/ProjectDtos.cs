using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Modules.Projects;

/// <summary>
/// Request model for creating a new project for a team.
/// </summary>
public record CreateProjectRequest(
    string Name,
    string Description,
    DateTime? StartDate,
    DateTime? EndDate);

/// <summary>
/// Request model for updating a project's details.
/// </summary>
public record UpdateProjectRequest(
    string Name,
    string Description,
    ProjectStatus Status,
    DateTime? StartDate,
    DateTime? EndDate);

/// <summary>
/// Response model for a project. Per docs/adr/0006-projects-definition.md,
/// integration data linking isn't decided yet, so this is just the
/// TeamHub-native record for now.
/// </summary>
public record ProjectResponse(
    Guid Id,
    Guid TeamId,
    string Name,
    string Description,
    ProjectStatus Status,
    DateTime? StartDate,
    DateTime? EndDate);
