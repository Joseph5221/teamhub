using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Modules.Users;

/// <summary>
/// Service for user profile data and system-wide (non-team-specific) role
/// assignment. See docs/adr/0005-auth-users-boundary.md for the boundary
/// with Auth, which owns credentials/sessions/tokens.
/// </summary>
public interface IUserService
{
    /// <summary>
    /// Gets a user's public profile. Any authenticated user may view any
    /// profile (a basic directory) — profile data here is already visible
    /// via team member lists.
    /// </summary>
    Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId);

    /// <summary>
    /// Updates the caller's own profile (display name, avatar). Self only.
    /// </summary>
    Task<Result<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);

    /// <summary>
    /// Assigns a user's system-wide role. Admin only.
    /// </summary>
    Task<Result<UserProfileResponse>> UpdateRoleAsync(Guid targetUserId, Guid requestingUserId, UpdateUserRoleRequest request);
}
