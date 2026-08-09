using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Teams;

/// <summary>
/// Service for team creation, membership, and settings management
/// </summary>
public interface ITeamService
{
    /// <summary>
    /// Creates a new team; the given user becomes its owner and first member
    /// </summary>
    Task<Result<TeamResponse>> CreateTeamAsync(Guid ownerId, CreateTeamRequest request);

    /// <summary>
    /// Gets a team's details. Only members (including the owner) may view it.
    /// </summary>
    Task<Result<TeamResponse>> GetTeamAsync(Guid teamId, Guid requestingUserId);

    /// <summary>
    /// Lists a team's members. Only members (including the owner) may view it.
    /// </summary>
    Task<Result<List<TeamMemberResponse>>> GetMembersAsync(Guid teamId, Guid requestingUserId);

    /// <summary>
    /// Updates team settings. Owner only.
    /// </summary>
    Task<Result<TeamResponse>> UpdateTeamAsync(Guid teamId, Guid requestingUserId, UpdateTeamRequest request);

    /// <summary>
    /// Adds an existing registered user to the team by email. Owner only.
    /// </summary>
    Task<Result<TeamMemberResponse>> AddMemberAsync(Guid teamId, Guid requestingUserId, AddTeamMemberRequest request);

    /// <summary>
    /// Removes a member from the team. Owner only; the owner cannot be removed this way.
    /// </summary>
    Task<Result> RemoveMemberAsync(Guid teamId, Guid requestingUserId, Guid memberUserId);
}
