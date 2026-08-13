using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Domain.Entities;

/// <summary>
/// Represents a project in the TeamHub system
/// </summary>
public class Project : BaseEntity
{
    /// <summary>
    /// Name of the project
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Detailed description of the project
    /// </summary>
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// Current status of the project
    /// </summary>
    public ProjectStatus Status { get; set; } = ProjectStatus.Planned;

    /// <summary>
    /// ID of the team this project belongs to
    /// </summary>
    public Guid TeamId { get; set; }
    
    /// <summary>
    /// The team this project belongs to
    /// </summary>
    public Team Team { get; set; } = null!;
    
    /// <summary>
    /// Project start date
    /// </summary>
    public DateTime? StartDate { get; set; }
    
    /// <summary>
    /// Project target completion date
    /// </summary>
    public DateTime? EndDate { get; set; }
}