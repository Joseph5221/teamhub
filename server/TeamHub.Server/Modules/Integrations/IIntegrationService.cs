using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// CRUD for a team's configured integrations, dispatching data/action
/// calls to the right <see cref="IModuleConnector"/> by
/// <see cref="Domain.Enums.IntegrationType"/>.
/// </summary>
public interface IIntegrationService
{
    /// <summary>
    /// Configures a new integration for a team. Owner only.
    /// </summary>
    Task<Result<IntegrationResponse>> CreateIntegrationAsync(Guid teamId, Guid requestingUserId, CreateIntegrationRequest request);

    /// <summary>
    /// Lists a team's configured integrations. Members only.
    /// </summary>
    Task<Result<List<IntegrationResponse>>> GetIntegrationsAsync(Guid teamId, Guid requestingUserId);

    /// <summary>
    /// Gets a single integration's details. Members only.
    /// </summary>
    Task<Result<IntegrationResponse>> GetIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId);

    /// <summary>
    /// Updates an integration's settings/configuration. Owner only.
    /// </summary>
    Task<Result<IntegrationResponse>> UpdateIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId, UpdateIntegrationRequest request);

    /// <summary>
    /// Removes an integration from a team. Owner only.
    /// </summary>
    Task<Result> DeleteIntegrationAsync(Guid teamId, Guid integrationId, Guid requestingUserId);

    /// <summary>
    /// Fetches normalized data for an integration from its connector.
    /// Members only.
    /// </summary>
    Task<Result<ModuleData>> GetIntegrationDataAsync(Guid teamId, Guid integrationId, Guid requestingUserId, DateTime? since = null);

    /// <summary>
    /// Invokes a connector-specific action (e.g. "sync") for an
    /// integration. Members only.
    /// </summary>
    Task<Result> InvokeIntegrationActionAsync(Guid teamId, Guid integrationId, Guid requestingUserId, InvokeIntegrationActionRequest request);
}
