using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Domain.Entities;

/// <summary>
/// Represents an integration (GitHub, Jira, etc.) in the TeamHub system
/// </summary>
public class Integration : BaseEntity
{
    /// <summary>
    /// Name of the integration (e.g., "GitHub", "Jira")
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Type of integration
    /// </summary>
    public IntegrationType Type { get; set; }

    /// <summary>
    /// Current connection status
    /// </summary>
    public IntegrationStatus Status { get; set; } = IntegrationStatus.Todo;

    /// <summary>
    /// Description of the integration
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Configuration data (JSON string for now)
    /// </summary>
    public string? ConfigurationData { get; set; }

    /// <summary>
    /// ID of the team this integration is configured for
    /// </summary>
    public Guid TeamId { get; set; }

    /// <summary>
    /// The team this integration is configured for
    /// </summary>
    public Team Team { get; set; } = null!;

    /// <summary>
    /// Whether the integration is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Last time this integration was synced
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}