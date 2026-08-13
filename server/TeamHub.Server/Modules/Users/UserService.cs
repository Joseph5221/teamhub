using TeamHub.Server.Domain.Common;
using TeamHub.Server.Domain.Entities;
using TeamHub.Server.Domain.Enums;
using TeamHub.Server.Infrastructure.Data;

namespace TeamHub.Server.Modules.Users;

/// <summary>
/// Implementation of user profile/role service
/// </summary>
public class UserService : IUserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public async Task<Result<UserProfileResponse>> GetProfileAsync(Guid userId)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<UserProfileResponse>.Failure(new Error("User.NotFound", $"User with ID {userId} was not found"));
        }

        return Result<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<Result<UserProfileResponse>> UpdateProfileAsync(Guid userId, UpdateProfileRequest request)
    {
        var user = await _context.Users.FindAsync(userId);
        if (user == null)
        {
            return Result<UserProfileResponse>.Failure(new Error("User.NotFound", $"User with ID {userId} was not found"));
        }

        if (string.IsNullOrWhiteSpace(request.Name))
        {
            return Result<UserProfileResponse>.Failure(new Error("User.Validation", "Name is required"));
        }

        if (!string.IsNullOrWhiteSpace(request.AvatarUrl) &&
            !Uri.TryCreate(request.AvatarUrl, UriKind.Absolute, out _))
        {
            return Result<UserProfileResponse>.Failure(new Error("User.Validation", "AvatarUrl must be a valid absolute URL"));
        }

        user.Name = request.Name;
        user.AvatarUrl = string.IsNullOrWhiteSpace(request.AvatarUrl) ? null : request.AvatarUrl;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<UserProfileResponse>.Success(ToProfileResponse(user));
    }

    public async Task<Result<UserProfileResponse>> UpdateRoleAsync(Guid targetUserId, Guid requestingUserId, UpdateUserRoleRequest request)
    {
        var requestingUser = await _context.Users.FindAsync(requestingUserId);
        if (requestingUser == null)
        {
            return Result<UserProfileResponse>.Failure(new Error("User.NotFound", $"User with ID {requestingUserId} was not found"));
        }

        if (requestingUser.Role != UserRole.Admin)
        {
            return Result<UserProfileResponse>.Failure(new Error("User.Forbidden", "Only an admin can assign roles"));
        }

        var targetUser = await _context.Users.FindAsync(targetUserId);
        if (targetUser == null)
        {
            return Result<UserProfileResponse>.Failure(new Error("User.NotFound", $"User with ID {targetUserId} was not found"));
        }

        targetUser.Role = request.Role;
        targetUser.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        return Result<UserProfileResponse>.Success(ToProfileResponse(targetUser));
    }

    private static UserProfileResponse ToProfileResponse(User user) =>
        new(user.Id, user.Name, user.Email, user.AvatarUrl, user.Role);
}
