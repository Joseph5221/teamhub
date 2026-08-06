namespace TeamHub.Server.Domain.Entities;

/// <summary>
/// Represents a team in the TeamHub system
/// </summary>
public class Team : BaseEntity
{
    /// <summary>
    /// Name of the team
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Description of the team's purpose
    /// </summary>
    public string Description { get; set; } = string.Empty;
    
    /// <summary>
    /// ID of the user who owns this team
    /// </summary>
    public Guid OwnerId { get; set; }
    
    /// <summary>
    /// The owner of this team
    /// </summary>
    public User Owner { get; set; } = null!;
    
    /// <summary>
    /// Members of this team
    /// </summary>
    public ICollection<User> Members { get; set; } = new List<User>();
    
    /// <summary>
    /// Projects belonging to this team
    /// </summary>
    public ICollection<Project> Projects { get; set; } = new List<Project>();

    /// <summary>
    /// Integrations configured for this team
    /// </summary>
    public ICollection<Integration> Integrations { get; set; } = new List<Integration>();
}