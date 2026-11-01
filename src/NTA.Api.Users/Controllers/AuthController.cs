using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTA.Api.Contracts;
using NTA.Core.Authentication;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;

namespace NTA.Api.Users.Controllers;

/// <summary>
/// Issues sample user-role tokens for seeded users.
/// </summary>
[ApiController]
[Route("auth")]
public sealed class AuthController(IUserRepository userRepository, IJwtTokenService tokenService) : ControllerBase
{
    /// <summary>
    /// Authenticates a seeded user and issues a token with the user role.
    /// </summary>
    [AllowAnonymous]
    [HttpPost("{userId:int}")]
    [ProducesResponseType<AuthenticationResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<AuthenticationResponse>> Authenticate(
        int userId,
        CancellationToken cancellationToken)
    {
        UserId id;
        try
        {
            id = new UserId(userId);
        }
        catch (ArgumentOutOfRangeException)
        {
            return NotFound();
        }

        if (await userRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        return Ok(new AuthenticationResponse(tokenService.CreateToken(id, [UserRoles.User])));
    }
}
