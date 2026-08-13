namespace TeamHub.Server.Domain.Enums;

/// <summary>
/// A user's system-wide role, independent of any team-specific role
/// (e.g. team owner/member — see Team.OwnerId/Team.Members). Owned by
/// the Users module per docs/adr/0005-auth-users-boundary.md.
/// </summary>
public enum UserRole
{
    Member,
    Admin
}
