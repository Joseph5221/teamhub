using TeamHub.Server.Domain.Enums;

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
    /// User's system-wide role (not team-specific — see Team.OwnerId/Members
    /// for team-scoped roles). Owned by the Users module.
    /// </summary>
    public UserRole Role { get; set; } = UserRole.Member;

    /// <summary>
    /// URL to the user's avatar image. Owned by the Users module; no file
    /// upload/blob storage yet, just a link the user supplies.
    /// </summary>
    public string? AvatarUrl { get; set; }

    /// <summary>
    /// Whether the user account is active
    /// </summary>
    public bool IsActive { get; set; } = true;
    
    /// <summary>
    /// Teams this user belongs to
    /// </summary>
    public ICollection<Team> Teams { get; set; } = new List<Team>();
}