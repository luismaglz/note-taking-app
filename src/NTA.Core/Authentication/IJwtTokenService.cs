using NTA.Core.ValueObjects;

namespace NTA.Core.Authentication;

/// <summary>
/// Defines token issuance for authenticated users.
/// </summary>
public interface IJwtTokenService
{
    /// <summary>
    /// Creates a signed token for the specified user and roles.
    /// </summary>
    string CreateToken(UserId userId, IReadOnlyCollection<string> roles);
}
