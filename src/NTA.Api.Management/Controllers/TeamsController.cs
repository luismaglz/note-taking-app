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
/// Provides manager-only CRUD and membership operations for teams.
/// </summary>
[ApiController]
[Route("teams")]
[Authorize(Roles = UserRoles.Manager)]
public sealed class TeamsController(
    ITeamRepository teamRepository,
    IUserRepository userRepository) : ControllerBase
{
    /// <summary>
    /// Creates a team.
    /// </summary>
    [HttpPost]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamResponse>> Create(
        CreateTeamRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var team = new Team(
                new TeamId(request.Id ?? await GetNextTeamIdAsync(cancellationToken)),
                request.Name);
            if (await teamRepository.GetByIdAsync(team.Id, cancellationToken) is not null)
            {
                return Conflict(new { error = $"Team '{team.Id.Value}' already exists." });
            }

            await teamRepository.AddAsync(team, cancellationToken);
            return CreatedAtAction(nameof(Get), new { teamId = team.Id.Value }, ManagementContractMapper.ToContract(team));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Lists teams.
    /// </summary>
    [HttpGet]
    [ProducesResponseType<IReadOnlyList<TeamResponse>>(StatusCodes.Status200OK)]
    public async Task<ActionResult<IReadOnlyList<TeamResponse>>> List(CancellationToken cancellationToken) =>
        Ok((await teamRepository.ListAsync(cancellationToken)).Select(ManagementContractMapper.ToContract).ToArray());

    /// <summary>
    /// Gets a team by identifier.
    /// </summary>
    [HttpGet("{teamId:int}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> Get(int teamId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        return team is null ? NotFound() : Ok(ManagementContractMapper.ToContract(team));
    }

    /// <summary>
    /// Updates a team.
    /// </summary>
    [HttpPut("{teamId:int}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<TeamResponse>> Update(
        int teamId,
        UpdateTeamRequest request,
        CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        try
        {
            team.Rename(request.Name);
            await teamRepository.UpdateAsync(team, cancellationToken);
            return Ok(ManagementContractMapper.ToContract(team));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Deletes an empty team.
    /// </summary>
    [HttpDelete("{teamId:int}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Delete(int teamId, CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        if (team is null)
        {
            return NotFound();
        }

        if (team.Members.Count > 0)
        {
            return Conflict(new { error = "A team with members cannot be deleted." });
        }

        await teamRepository.DeleteAsync(team.Id, cancellationToken);
        return NoContent();
    }

    /// <summary>
    /// Lists the members of a team.
    /// </summary>
    [HttpGet("{teamId:int}/members")]
    [ProducesResponseType<IReadOnlyList<TeamMemberResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<IReadOnlyList<TeamMemberResponse>>> ListMembers(
        int teamId,
        CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        return team is null
            ? NotFound()
            : Ok(team.Members.Order().Select(member => new TeamMemberResponse(member.Value)).ToArray());
    }

    /// <summary>
    /// Adds an existing user to a team.
    /// </summary>
    [HttpPost("{teamId:int}/members")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamResponse>> AddMember(
        int teamId,
        AddTeamMemberRequest request,
        CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        if (team is null || !TryCreateUserId(request.UserId, out var userId) ||
            await userRepository.GetByIdAsync(userId, cancellationToken) is null)
        {
            return NotFound();
        }

        try
        {
            team.AddMember(userId);
            await teamRepository.UpdateAsync(team, cancellationToken);
            return Ok(ManagementContractMapper.ToContract(team));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    /// <summary>
    /// Removes a member from a team.
    /// </summary>
    [HttpDelete("{teamId:int}/members/{userId:int}")]
    [ProducesResponseType<TeamResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<TeamResponse>> RemoveMember(
        int teamId,
        int userId,
        CancellationToken cancellationToken)
    {
        var team = await FindTeamAsync(teamId, cancellationToken);
        if (team is null || !TryCreateUserId(userId, out var memberId))
        {
            return NotFound();
        }

        try
        {
            team.RemoveMember(memberId);
            await teamRepository.UpdateAsync(team, cancellationToken);
            return Ok(ManagementContractMapper.ToContract(team));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
    }

    private async Task<Team?> FindTeamAsync(int value, CancellationToken cancellationToken)
    {
        if (value <= 0)
        {
            return null;
        }

        return await teamRepository.GetByIdAsync(new TeamId(value), cancellationToken);
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

    private async Task<int> GetNextTeamIdAsync(CancellationToken cancellationToken)
    {
        var teams = await teamRepository.ListAsync(cancellationToken);
        return teams.Count == 0 ? 1 : teams.Max(team => team.Id.Value) + 1;
    }
}
