using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Infrastructure.Security;

/// <summary>
/// Validates that a plaintext password meets the minimum strength policy
/// before it's hashed and stored
/// </summary>
public interface IPasswordValidator
{
    Result Validate(string password);
}
