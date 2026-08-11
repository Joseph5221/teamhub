using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Modules.Integrations;

/// <summary>
/// Request model for configuring a new integration for a team.
/// <see cref="Configuration"/> is connector-specific (e.g. GitHub expects
/// "organization" and optionally "personalAccessToken") and is stored as-is;
/// it is never echoed back in a response.
/// </summary>
public record CreateIntegrationRequest(
    string Name,
    IntegrationType Type,
    string Description,
    Dictionary<string, string>? Configuration);

/// <summary>
/// Request model for updating an integration's settings/configuration.
/// </summary>
public record UpdateIntegrationRequest(
    string Name,
    string Description,
    bool IsEnabled,
    Dictionary<string, string>? Configuration);

/// <summary>
/// Request model for invoking a connector-specific action (e.g. "sync").
/// </summary>
public record InvokeIntegrationActionRequest(string Action);

/// <summary>
/// Response model for an integration. Never includes raw
/// <c>ConfigurationData</c> (may contain secrets) — only whether it's
/// configured, via <see cref="IsConfigured"/>.
/// </summary>
public record IntegrationResponse(
    Guid Id,
    Guid TeamId,
    string Name,
    IntegrationType Type,
    IntegrationStatus Status,
    string Description,
    bool IsEnabled,
    bool IsConfigured,
    DateTime? LastSyncedAt);
