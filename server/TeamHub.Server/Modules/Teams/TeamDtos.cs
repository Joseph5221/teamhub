namespace TeamHub.Server.Modules.Teams;

/// <summary>
/// Request model for creating a team. The caller becomes the team's owner.
/// </summary>
public record CreateTeamRequest(string Name, string Description);

/// <summary>
/// Request model for updating team settings
/// </summary>
public record UpdateTeamRequest(string Name, string Description);

/// <summary>
/// Request model for adding an existing user to a team by email
/// </summary>
public record AddTeamMemberRequest(string Email);

/// <summary>
/// A team member with their role ("Owner" or "Member")
/// </summary>
public record TeamMemberResponse(Guid UserId, string Name, string Email, string Role);

/// <summary>
/// Response model for team details
/// </summary>
public record TeamResponse(
    Guid Id,
    string Name,
    string Description,
    Guid OwnerId,
    List<TeamMemberResponse> Members
);
