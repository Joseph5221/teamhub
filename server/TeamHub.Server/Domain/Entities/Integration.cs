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
    public string Type { get; set; } = string.Empty;
    
    /// <summary>
    /// Current status (TODO, InProgress, Connected, Failed)
    /// </summary>
    public string Status { get; set; } = "TODO";
    
    /// <summary>
    /// Description of the integration
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// Configuration data (JSON string for now)
    /// </summary>
    public string? ConfigurationData { get; set; }
    
    /// <summary>
    /// ID of the user who owns this integration
    /// </summary>
    public Guid UserId { get; set; }
    
    /// <summary>
    /// The user who owns this integration
    /// </summary>
    public User User { get; set; } = null!;
    
    /// <summary>
    /// Whether the integration is enabled
    /// </summary>
    public bool IsEnabled { get; set; } = true;
    
    /// <summary>
    /// Last time this integration was synced
    /// </summary>
    public DateTime? LastSyncedAt { get; set; }
}