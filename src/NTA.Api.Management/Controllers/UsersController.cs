using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTA.Api.Contracts;
using NTA.Api.Management.Mappings;
using NTA.Core.Authentication;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;

namespace NTA.Api.Management.Controllers;

/// <summary>
/// Provides manager-only CRUD operations for users.
/// </summary>
[ApiController]
[Route("users")]
[Authorize(Roles = UserRoles.Manager)]
public sealed class UsersController(
    IUserRepository userRepository,
    ITeamRepository teamRepository) : ControllerBase
{
    /// <summary>
    /// Creates a user.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<UserResponse>> Create(
        CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var user = new User(
                new UserId(request.Id ?? await GetNextUserIdAsync(cancellationToken)),
                request.Name);
            if (await userRepository.GetByIdAsync(user.Id, cancellationToken) is not null)
            {
                return Conflict(new { error = $"User '{user.Id.Value}' already exists." });
            }

            await userRepository.AddAsync(user, cancellationToken);
            return CreatedAtAction(nameof(Get), new { userId = user.Id.Value }, ManagementContractMapper.ToContract(user));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Lists users.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<UserResponse>>> List(CancellationToken cancellationToken) =>
        Ok((await userRepository.ListAsync(cancellationToken)).Select(ManagementContractMapper.ToContract).ToArray());

    /// <summary>
    /// Gets a user by identifier.
    /// </summary>
    [HttpGet("{userId:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Get(int userId, CancellationToken cancellationToken)
    {
        if (!TryCreateUserId(userId, out var id))
        {
            return NotFound();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        return user is null ? NotFound() : Ok(ManagementContractMapper.ToContract(user));
    }

    /// <summary>
    /// Updates a user.
    /// </summary>
    [HttpPut("{userId:int}")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<UserResponse>> Update(
        int userId,
        UpdateUserRequest request,
        CancellationToken cancellationToken)
    {
        if (!TryCreateUserId(userId, out var id))
        {
            return NotFound();
        }

        var user = await userRepository.GetByIdAsync(id, cancellationToken);
        if (user is null)
        {
            return NotFound();
        }

        try
        {
            user.Rename(request.Name);
            await userRepository.UpdateAsync(user, cancellationToken);
            return Ok(ManagementContractMapper.ToContract(user));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Deletes a user that does not belong to a team.
    /// </summary>
    [HttpDelete("{userId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int userId, CancellationToken cancellationToken)
    {
        if (!TryCreateUserId(userId, out var id) ||
            await userRepository.GetByIdAsync(id, cancellationToken) is null)
        {
            return NotFound();
        }

        if (await teamRepository.HasMemberAsync(id, cancellationToken))
        {
            return Conflict(new { error = "A user assigned to a team cannot be deleted." });
        }

        await userRepository.DeleteAsync(id, cancellationToken);
        return NoContent();
    }

    private static bool TryCreateUserId(int value, out UserId id)
    {
        if (value > 0)
        {
            id = new UserId(value);
            return true;
        }

        id = default;
        return false;
    }

    private async Task<int> GetNextUserIdAsync(CancellationToken cancellationToken)
    {
        var users = await userRepository.ListAsync(cancellationToken);
        return users.Count == 0 ? 1 : users.Max(user => user.Id.Value) + 1;
    }
}
