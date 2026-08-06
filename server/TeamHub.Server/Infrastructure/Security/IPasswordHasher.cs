namespace TeamHub.Server.Infrastructure.Security;

/// <summary>
/// Hashes and verifies user passwords
/// </summary>
public interface IPasswordHasher
{
    /// <summary>
    /// Hashes a plaintext password for storage
    /// </summary>
    string HashPassword(string password);

    /// <summary>
    /// Verifies a plaintext password against a previously hashed password
    /// </summary>
    bool VerifyPassword(string hashedPassword, string providedPassword);
}
