using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Modules.Teams;

/// <summary>
/// Implementation of team service
/// </summary>
public class TeamService : ITeamService
{
    private readonly AppDbContext _context;

    public TeamService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<TeamResponse>> CreateTeamAsync(Guid ownerId, CreateTeamRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamResponse>.Failure(new Error("Team.Validation", "Team name is required"));
        }

        var owner = await _context.Users.FindAsync(ownerId);
        if (owner == null)
        {
            return Result<TeamResponse>.Failure(new Error("Team.UserNotFound", "Owner user was not found"));
        }

        var team = new Team
        {
            Name = request.Name,
            Description = request.Description,
            Owner = owner,
            Members = { owner }
        };

        _context.Teams.Add(team);
        await _context.SaveChangesAsync();

        return Result<TeamResponse>.Success(ToTeamResponse(team));
    }

    public async Task<Result<TeamResponse>> GetTeamAsync(Guid teamId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<TeamResponse>.Failure(new Error("Team.NotFound", $"Team with ID {teamId} was not found"));
        }

        if (!IsMember(team, requestingUserId))
        {
            return Result<TeamResponse>.Failure(new Error("Team.Forbidden", "You are not a member of this team"));
        }

        return Result<TeamResponse>.Success(ToTeamResponse(team));
    }

    public async Task<Result<List<TeamMemberResponse>>> GetMembersAsync(Guid teamId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<List<TeamMemberResponse>>.Failure(new Error("Team.NotFound", $"Team with ID {teamId} was not found"));
        }

        if (!IsMember(team, requestingUserId))
        {
            return Result<List<TeamMemberResponse>>.Failure(new Error("Team.Forbidden", "You are not a member of this team"));
        }

        return Result<List<TeamMemberResponse>>.Success(ToMemberResponses(team));
    }

    public async Task<Result<TeamResponse>> UpdateTeamAsync(Guid teamId, Guid requestingUserId, UpdateTeamRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<TeamResponse>.Failure(new Error("Team.NotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<TeamResponse>.Failure(new Error("Team.Forbidden", "Only the team owner can update team settings"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<TeamResponse>.Failure(new Error("Team.Validation", "Team name is required"));
        }

        team.Name = request.Name;
        team.Description = request.Description;
        team.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<TeamResponse>.Success(ToTeamResponse(team));
    }

    public async Task<Result<TeamMemberResponse>> AddMemberAsync(Guid teamId, Guid requestingUserId, AddTeamMemberRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<TeamMemberResponse>.Failure(new Error("Team.NotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<TeamMemberResponse>.Failure(new Error("Team.Forbidden", "Only the team owner can add members"));
        }

        var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == request.Email);
        if (user == null)
        {
            return Result<TeamMemberResponse>.Failure(new Error("Team.UserNotFound", "No user with this email was found"));
        }

        if (team.Members.Any(m => m.Id == user.Id))
        {
            return Result<TeamMemberResponse>.Failure(new Error("Team.AlreadyMember", "User is already a member of this team"));
        }

        team.Members.Add(user);
        await _context.SaveChangesAsync();

        return Result<TeamMemberResponse>.Success(ToMemberResponse(team, user));
    }

    public async Task<Result> RemoveMemberAsync(Guid teamId, Guid requestingUserId, Guid memberUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result.Failure(new Error("Team.NotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result.Failure(new Error("Team.Forbidden", "Only the team owner can remove members"));
        }

        if (memberUserId == team.OwnerId)
        {
            return Result.Failure(new Error("Team.CannotRemoveOwner", "The team owner cannot be removed"));
        }

        var member = team.Members.FirstOrDefault(m => m.Id == memberUserId);
        if (member == null)
        {
            return Result.Failure(new Error("Team.MemberNotFound", "User is not a member of this team"));
        }

        team.Members.Remove(member);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    private async Task<Team?> LoadTeamAsync(Guid teamId)
    {
        return await _context.Teams
            .Include(t => t.Members)
            .FirstOrDefaultAsync(t => t.Id == teamId);
    }

    private static bool IsMember(Team team, Guid userId) =>
        team.OwnerId == userId || team.Members.Any(m => m.Id == userId);

    private static TeamResponse ToTeamResponse(Team team) =>
        new(team.Id, team.Name, team.Description, team.OwnerId, ToMemberResponses(team));

    private static List<TeamMemberResponse> ToMemberResponses(Team team) =>
        team.Members
            .Select(m => ToMemberResponse(team, m))
            .OrderBy(m => m.Name)
            .ToList();

    private static TeamMemberResponse ToMemberResponse(Team team, User user) =>
        new(user.Id, user.Name, user.Email, user.Id == team.OwnerId ? "Owner" : "Member");
}
