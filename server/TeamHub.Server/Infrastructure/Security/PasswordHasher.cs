using Microsoft.AspNetCore.Identity;
using TeamHub.Server.Domain.Entities;

namespace TeamHub.Server.Infrastructure.Security;

/// <summary>
/// Hashes and verifies passwords using ASP.NET Core Identity's PBKDF2-based
/// PasswordHasher. The TUser generic is unused by the algorithm itself, so a
/// placeholder User instance is passed at each call site.
/// </summary>
public class PasswordHasher : IPasswordHasher
{
    private readonly Microsoft.AspNetCore.Identity.PasswordHasher<User> _hasher = new();

    public string HashPassword(string password)
    {
        return _hasher.HashPassword(new User(), password);
    }

    public bool VerifyPassword(string hashedPassword, string providedPassword)
    {
        var result = _hasher.VerifyHashedPassword(new User(), hashedPassword, providedPassword);
        return result is PasswordVerificationResult.Success
            or PasswordVerificationResult.SuccessRehashNeeded;
    }
}
