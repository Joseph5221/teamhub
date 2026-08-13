using TeamHub.Server.Domain.Enums;

namespace TeamHub.Server.Modules.Users;

/// <summary>
/// A user's public profile
/// </summary>
public record UserProfileResponse(
    Guid Id,
    string Name,
    string Email,
    string? AvatarUrl,
    UserRole Role
);

/// <summary>
/// Request model for updating the caller's own profile
/// </summary>
public record UpdateProfileRequest(string Name, string? AvatarUrl);

/// <summary>
/// Request model for assigning a user's system-wide role. Admin only.
/// </summary>
public record UpdateUserRoleRequest(UserRole Role);
