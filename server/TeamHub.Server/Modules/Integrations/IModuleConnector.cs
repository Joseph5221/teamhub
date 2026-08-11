using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// Shared contract every integration submodule (GitHub, Jira, Slack, ...)
/// implements so <see cref="IIntegrationService"/> can dispatch to them
/// interchangeably by <see cref="IntegrationType"/>. See
/// docs/ARCHITECTURE.md "Module Interface Contract".
/// </summary>
public interface IModuleConnector
{
    /// <summary>
    /// The integration type this connector handles. Used by
    /// <see cref="IIntegrationService"/> to route a team's configured
    /// integration to the right connector.
    /// </summary>
    IntegrationType Type { get; }

    /// <summary>
    /// Returns the connector's current configuration status for a team
    /// (e.g. whether credentials are set) without exposing secret values.
    /// </summary>
    Task<ModuleConfig> GetConfigAsync(Guid teamId);

    /// <summary>
    /// Fetches normalized data from the external system for a team,
    /// optionally limited to items changed since <paramref name="since"/>.
    /// </summary>
    Task<ModuleData> GetDataAsync(Guid teamId, DateTime? since = null);

    /// <summary>
    /// Invokes a connector-specific action (e.g. "sync") for a team.
    /// </summary>
    Task InvokeActionAsync(Guid teamId, string action);
}

/// <summary>
/// A connector's configuration status for one team. <see cref="Fields"/>
/// holds non-secret, display-safe values only (e.g. an org name) — never
/// tokens or credentials.
/// </summary>
public record ModuleConfig(bool IsConfigured, IReadOnlyDictionary<string, string?> Fields);

/// <summary>
/// Normalized data pulled from an external system, generic enough to cover
/// GitHub repos today and Jira issues/Slack messages later.
/// </summary>
public record ModuleData(DateTime FetchedAt, IReadOnlyList<ModuleDataItem> Items);

/// <summary>
/// One normalized item within a <see cref="ModuleData"/> payload (e.g. a
/// GitHub repository).
/// </summary>
public record ModuleDataItem(
    string Id,
    string Title,
    string? Description,
    string? Url,
    DateTime? UpdatedAt,
    IReadOnlyDictionary<string, string>? Metadata = null);
