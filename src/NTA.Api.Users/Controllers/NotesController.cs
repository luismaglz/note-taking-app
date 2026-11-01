using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTA.Api.Contracts;
using NTA.Api.Users.Mappings;
using NTA.Core.Authentication;
using NTA.Core.Context;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;

namespace NTA.Api.Users.Controllers;

/// <summary>
/// Provides CRUD operations for user notes.
/// </summary>
[ApiController]
[Route("notes")]
[Authorize(Roles = UserRoles.User)]
public sealed class NotesController(
    INoteRepository repository,
    INoteAccessPolicyRepository policyRepository,
    ITeamRepository teamRepository,
    TimeProvider timeProvider,
    UserContext userContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<NoteResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<NoteResponse>> Create(
        CreateNoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = Note.Create(
                userContext.GetRequiredUserId(),
                request.Title,
                request.Content,
                timeProvider.GetUtcNow());

            await repository.AddAsync(note, cancellationToken);
            await policyRepository.AddAsync(NoteAccessPolicy.Create(note.Id), cancellationToken);
            return CreatedAtAction(nameof(Get), new { noteId = note.Id.Value }, NoteContractMapper.ToContract(note));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet]
    [ProducesResponseType<IReadOnlyList<NoteResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<ActionResult<IReadOnlyList<NoteResponse>>> List(CancellationToken cancellationToken)
    {
        try
        {
            var userId = userContext.GetRequiredUserId();
            var notes = await repository.ListVisibleToAsync(userId, cancellationToken);
            return Ok(notes.Select(NoteContractMapper.ToContract).ToArray());
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpGet("{noteId:guid}")]
    [ProducesResponseType<NoteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Get(Guid noteId, CancellationToken cancellationToken)
    {
        try
        {
            var userId = userContext.GetRequiredUserId();
            var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null)
            {
                return NotFound();
            }

            var teamIds = (await teamRepository.ListByMemberAsync(userId, cancellationToken))
                .Select(team => team.Id)
                .ToHashSet();
            if (!await HasAccessAsync(note, userId, teamIds, NotePermission.Read, cancellationToken))
            {
                return NotFound();
            }

            return Ok(NoteContractMapper.ToContract(note));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpPut("{noteId:guid}")]
    [ProducesResponseType<NoteResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<ActionResult<NoteResponse>> Update(
        Guid noteId,
        UpdateNoteRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null)
            {
                return NotFound();
            }

            var userId = userContext.GetRequiredUserId();
            var teamIds = (await teamRepository.ListByMemberAsync(userId, cancellationToken))
                .Select(team => team.Id)
                .ToHashSet();
            if (!await HasAccessAsync(note, userId, teamIds, NotePermission.Write, cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            note.Update(
                request.Title,
                request.Content,
                timeProvider.GetUtcNow());
            await repository.UpdateAsync(note, cancellationToken);

            return Ok(NoteContractMapper.ToContract(note));
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    [HttpDelete("{noteId:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(Guid noteId, CancellationToken cancellationToken)
    {
        try
        {
            var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null)
            {
                return NotFound();
            }

            var userId = userContext.GetRequiredUserId();
            var teamIds = (await teamRepository.ListByMemberAsync(userId, cancellationToken))
                .Select(team => team.Id)
                .ToHashSet();
            if (!await HasAccessAsync(note, userId, teamIds, NotePermission.Delete, cancellationToken))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            await policyRepository.DeleteAsync(note.Id, cancellationToken);
            await repository.DeleteAsync(note.Id, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private async Task<bool> HasAccessAsync(
        Note note,
        UserId userId,
        IReadOnlySet<TeamId> teamIds,
        NotePermission permission,
        CancellationToken cancellationToken)
    {
        if (note.OwnerId == userId)
        {
            return true;
        }

        var policy = await policyRepository.GetByNoteIdAsync(note.Id, cancellationToken);
        return policy?.HasPermission(userId, teamIds, permission) ?? false;
    }
}
