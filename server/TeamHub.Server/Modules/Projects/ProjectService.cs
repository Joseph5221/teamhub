using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Modules.Projects;

/// <summary>
/// Implementation of <see cref="IProjectService"/>. Follows the same
/// owner/member permission shape as <c>TeamService</c>/<c>IntegrationService</c>:
/// the owner manages projects (create/update/delete), any member can view.
/// </summary>
public class ProjectService : IProjectService
{
    private readonly AppDbContext _context;

    public ProjectService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<ProjectResponse>> CreateProjectAsync(Guid teamId, Guid requestingUserId, CreateProjectRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.Forbidden", "Only the team owner can create projects"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ProjectResponse>.Failure(new Error("Project.Validation", "Project name is required"));
        }

        var project = new Project
        {
            Name = request.Name,
            Description = request.Description,
            TeamId = teamId,
            Team = team,
            StartDate = request.StartDate,
            EndDate = request.EndDate
        };

        _context.Projects.Add(project);
        await _context.SaveChangesAsync();

        return Result<ProjectResponse>.Success(ToResponse(project));
    }

    public async Task<Result<List<ProjectResponse>>> GetProjectsAsync(Guid teamId, Guid requestingUserId)
    {
        var membership = await CheckMembershipAsync(teamId, requestingUserId);
        if (membership.IsFailure)
        {
            return Result<List<ProjectResponse>>.Failure(membership.Error!);
        }

        var projects = await _context.Projects
            .Where(p => p.TeamId == teamId)
            .OrderBy(p => p.Name)
            .ToListAsync();

        return Result<List<ProjectResponse>>.Success(projects.Select(ToResponse).ToList());
    }

    public async Task<Result<ProjectResponse>> GetProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId)
    {
        var membership = await CheckMembershipAsync(teamId, requestingUserId);
        if (membership.IsFailure)
        {
            return Result<ProjectResponse>.Failure(membership.Error!);
        }

        var project = await FindProjectAsync(teamId, projectId);
        if (project == null)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.NotFound", $"Project with ID {projectId} was not found"));
        }

        return Result<ProjectResponse>.Success(ToResponse(project));
    }

    public async Task<Result<ProjectResponse>> UpdateProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId, UpdateProjectRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.Forbidden", "Only the team owner can update projects"));
        }

        var project = await FindProjectAsync(teamId, projectId);
        if (project == null)
        {
            return Result<ProjectResponse>.Failure(new Error("Project.NotFound", $"Project with ID {projectId} was not found"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<ProjectResponse>.Failure(new Error("Project.Validation", "Project name is required"));
        }

        project.Name = request.Name;
        project.Description = request.Description;
        project.Status = request.Status;
        project.StartDate = request.StartDate;
        project.EndDate = request.EndDate;
        project.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result<ProjectResponse>.Success(ToResponse(project));
    }

    public async Task<Result> DeleteProjectAsync(Guid teamId, Guid projectId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result.Failure(new Error("Project.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result.Failure(new Error("Project.Forbidden", "Only the team owner can remove projects"));
        }

        var project = await FindProjectAsync(teamId, projectId);
        if (project == null)
        {
            return Result.Failure(new Error("Project.NotFound", $"Project with ID {projectId} was not found"));
        }

        _context.Projects.Remove(project);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<Result> CheckMembershipAsync(Guid teamId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result.Failure(new Error("Project.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (!IsMember(team, requestingUserId))
        {
            return Result.Failure(new Error("Project.Forbidden", "You are not a member of this team"));
        }

        return Result.Success();
    }

    private async Task<Team?> LoadTeamAsync(Guid teamId) =>
        await _context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == teamId);

    private Task<Project?> FindProjectAsync(Guid teamId, Guid projectId) =>
        _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.TeamId == teamId);

    private static bool IsMember(Team team, Guid userId) =>
        team.OwnerId == userId || team.Members.Any(m => m.Id == userId);

    private static ProjectResponse ToResponse(Project project) => new(
        project.Id,
        project.TeamId,
        project.Name,
        project.Description,
        project.Status,
        project.StartDate,
        project.EndDate);
}
