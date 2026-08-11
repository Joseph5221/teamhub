using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// Implementation of <see cref="IIntegrationService"/>. Follows the same
/// owner/member permission shape as <c>TeamService</c>: the owner manages
/// config (create/update/delete), any member can view and trigger
/// data/action calls.
/// </summary>
public class IntegrationService : IIntegrationService
{
    private readonly AppDbContext _context;
    private readonly IReadOnlyDictionary<Domain.Enums.IntegrationType, IModuleConnector> _connectors;

    public IntegrationService(AppDbContext context, IEnumerable<IModuleConnector> connectors)
    {
        _context = context;
        _connectors = connectors.ToDictionary(c => c.Type);
    }

    public async Task<Result<IntegrationResponse>> CreateIntegrationAsync(Guid teamId, Guid requestingUserId, CreateIntegrationRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.Forbidden", "Only the team owner can configure integrations"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.Validation", "Integration name is required"));
        }

        var integration = new Integration
        {
            Name = request.Name,
            Type = request.Type,
            Description = request.Description,
            TeamId = teamId,
            Team = team,
            ConfigurationData = SerializeConfiguration(request.Configuration)
        };

        _context.Integrations.Add(integration);
        await _context.SaveChangesAsync();

        return Result<IntegrationResponse>.Success(ToResponse(integration));
    }

    public async Task<Result<List<IntegrationResponse>>> GetIntegrationsAsync(Guid teamId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<List<IntegrationResponse>>.Failure(new Error("Integration.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (!IsMember(team, requestingUserId))
        {
            return Result<List<IntegrationResponse>>.Failure(new Error("Integration.Forbidden", "You are not a member of this team"));
        }

        var integrations = await _context.Integrations
            .Where(i => i.TeamId == teamId)
            .OrderBy(i => i.Name)
            .ToListAsync();

        return Result<List<IntegrationResponse>>.Success(integrations.Select(ToResponse).ToList());
    }

    public async Task<Result<IntegrationResponse>> GetIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId)
    {
        var membership = await CheckMembershipAsync(teamId, requestingUserId);
        if (membership.IsFailure)
        {
            return Result<IntegrationResponse>.Failure(membership.Error!);
        }

        var integration = await FindIntegrationAsync(teamId, integrationId);
        if (integration == null)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.NotFound", $"Integration with ID {integrationId} was not found"));
        }

        return Result<IntegrationResponse>.Success(ToResponse(integration));
    }

    public async Task<Result<IntegrationResponse>> UpdateIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId, UpdateIntegrationRequest request)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.Forbidden", "Only the team owner can update integration settings"));
        }

        var integration = await FindIntegrationAsync(teamId, integrationId);
        if (integration == null)
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.NotFound", $"Integration with ID {integrationId} was not found"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<IntegrationResponse>.Failure(new Error("Integration.Validation", "Integration name is required"));
        }

        integration.Name = request.Name;
        integration.Description = request.Description;
        integration.IsEnabled = request.IsEnabled;
        integration.ConfigurationData = SerializeConfiguration(request.Configuration);
        integration.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Result<IntegrationResponse>.Success(ToResponse(integration));
    }

    public async Task<Result> DeleteIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result.Failure(new Error("Integration.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (team.OwnerId != requestingUserId)
        {
            return Result.Failure(new Error("Integration.Forbidden", "Only the team owner can remove integrations"));
        }

        var integration = await FindIntegrationAsync(teamId, integrationId);
        if (integration == null)
        {
            return Result.Failure(new Error("Integration.NotFound", $"Integration with ID {integrationId} was not found"));
        }

        _context.Integrations.Remove(integration);
        await _context.SaveChangesAsync();

        return Result.Success();
    }

    public async Task<Result<ModuleData>> GetIntegrationDataAsync(Guid teamId, Guid integrationId, Guid requestingUserId, DateTime? since = null)
    {
        var membership = await CheckMembershipAsync(teamId, requestingUserId);
        if (membership.IsFailure)
        {
            return Result<ModuleData>.Failure(membership.Error!);
        }

        var integration = await FindIntegrationAsync(teamId, integrationId);
        if (integration == null)
        {
            return Result<ModuleData>.Failure(new Error("Integration.NotFound", $"Integration with ID {integrationId} was not found"));
        }

        if (!_connectors.TryGetValue(integration.Type, out var connector))
        {
            return Result<ModuleData>.Failure(new Error("Integration.NotSupported", $"No connector is implemented yet for integration type '{integration.Type}'"));
        }

        try
        {
            var data = await connector.GetDataAsync(teamId, since);
            return Result<ModuleData>.Success(data);
        }
        catch (ModuleConnectorException ex)
        {
            integration.Status = Domain.Enums.IntegrationStatus.Failed;
            await _context.SaveChangesAsync();
            return Result<ModuleData>.Failure(new Error(ex.Code, ex.Message));
        }
    }

    public async Task<Result> InvokeIntegrationActionAsync(Guid teamId, Guid integrationId, Guid requestingUserId, InvokeIntegrationActionRequest request)
    {
        var membership = await CheckMembershipAsync(teamId, requestingUserId);
        if (membership.IsFailure)
        {
            return Result.Failure(membership.Error!);
        }

        var integration = await FindIntegrationAsync(teamId, integrationId);
        if (integration == null)
        {
            return Result.Failure(new Error("Integration.NotFound", $"Integration with ID {integrationId} was not found"));
        }

        if (!_connectors.TryGetValue(integration.Type, out var connector))
        {
            return Result.Failure(new Error("Integration.NotSupported", $"No connector is implemented yet for integration type '{integration.Type}'"));
        }

        try
        {
            await connector.InvokeActionAsync(teamId, request.Action);
            return Result.Success();
        }
        catch (ModuleConnectorException ex)
        {
            integration.Status = Domain.Enums.IntegrationStatus.Failed;
            await _context.SaveChangesAsync();
            return Result.Failure(new Error(ex.Code, ex.Message));
        }
    }

    private async Task<Result> CheckMembershipAsync(Guid teamId, Guid requestingUserId)
    {
        var team = await LoadTeamAsync(teamId);
        if (team == null)
        {
            return Result.Failure(new Error("Integration.TeamNotFound", $"Team with ID {teamId} was not found"));
        }

        if (!IsMember(team, requestingUserId))
        {
            return Result.Failure(new Error("Integration.Forbidden", "You are not a member of this team"));
        }

        return Result.Success();
    }

    private async Task<Team?> LoadTeamAsync(Guid teamId) =>
        await _context.Teams.Include(t => t.Members).FirstOrDefaultAsync(t => t.Id == teamId);

    private Task<Integration?> FindIntegrationAsync(Guid teamId, Guid integrationId) =>
        _context.Integrations.FirstOrDefaultAsync(i => i.Id == integrationId && i.TeamId == teamId);

    private static bool IsMember(Team team, Guid userId) =>
        team.OwnerId == userId || team.Members.Any(m => m.Id == userId);

    private static string? SerializeConfiguration(Dictionary<string, string>? configuration) =>
        configuration == null || configuration.Count == 0 ? null : JsonSerializer.Serialize(configuration);

    private static IntegrationResponse ToResponse(Integration integration) => new(
        integration.Id,
        integration.TeamId,
        integration.Name,
        integration.Type,
        integration.Status,
        integration.Description,
        integration.IsEnabled,
        !string.IsNullOrWhiteSpace(integration.ConfigurationData),
        integration.LastSyncedAt);
}
