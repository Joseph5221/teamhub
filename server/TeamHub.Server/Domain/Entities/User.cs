namespace TeamHub.Server.Domain.Entities;

/// <summary>
/// Represents a user in the TeamHub system
/// </summary>
public class User : BaseEntity
{
    /// <summary>
    /// User's email address (used for login)
    /// </summary>
    public string Email { get; set; } = string.Empty;
    
    /// <summary>
    /// User's display name
    /// </summary>
    public string Name { get; set; } = string.Empty;
    
    /// <summary>
    /// Hashed password (will be used later for real auth)
    /// </summary>
    public string PasswordHash { get; set; } = string.Empty;
    
    /// <summary>
    /// User's role in the system
    /// </summary>
    public string Role { get; set; } = "User";
    
    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Teams this user belongs to
    /// </summary>
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}