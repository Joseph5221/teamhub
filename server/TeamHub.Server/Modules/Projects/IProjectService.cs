using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Projects;

/// <summary>
/// Service for team-scoped project CRUD. See
/// docs/adr/0006-projects-definition.md for what a "Project" is.
/// </summary>
public interface IProjectService
{
    /// <summary>
    /// Creates a new project for a team. Owner only.
    /// </summary>
    Task<Result<ProjectResponse>> CreateProjectAsync(Guid teamId, Guid requestingUserId, CreateProjectRequest request);

    /// <summary>
    /// Lists a team's projects. Members only.
    /// </summary>
    Task<Result<List<ProjectResponse>>> GetProjectsAsync(Guid teamId, Guid requestingUserId);

    /// <summary>
    /// Gets a single project's details. Members only.
    /// </summary>
    Task<Result<ProjectResponse>> GetProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId);

    /// <summary>
    /// Updates a project's details. Owner only.
    /// </summary>
    Task<Result<ProjectResponse>> UpdateProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId, UpdateProjectRequest request);

    /// <summary>
    /// Removes a project from a team. Owner only.
    /// </summary>
    Task<Result> DeleteProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId);
}
