using TeamHub.Server.Domain.Common;

namespace TeamHub.Server.Infrastructure.Security;

/// <summary>
/// Minimum password policy: 8-128 characters, at least one letter and one digit.
/// Deliberately doesn't require special characters — length is a stronger
/// predictor of strength and special-character rules push users toward
/// predictable substitutions (NIST SP 800-63B).
/// </summary>
public class PasswordValidator : IPasswordValidator
{
    private const int MinLength = 8;
    private const int MaxLength = 128;

    public Result Validate(string password)
    {
        if (string.IsNullOrEmpty(password))
        {
            return Result.Failure(new Error("Auth.WeakPassword",
                $"Password must be at least {MinLength} characters long"));
        }

        var errors = new List<string>();

        if (password.Length < MinLength)
        {
            errors.Add($"Password must be at least {MinLength} characters long");
        }

        if (password.Length > MaxLength)
        {
            errors.Add($"Password must be no more than {MaxLength} characters long");
        }

        if (!password.Any(char.IsLetter))
        {
            errors.Add("Password must contain at least one letter");
        }

        if (!password.Any(char.IsDigit))
        {
            errors.Add("Password must contain at least one number");
        }

        return errors.Count == 0
            ? Result.Success()
            : Result.Failure(new Error("Auth.WeakPassword", string.Join(" ", errors)));
    }
}
