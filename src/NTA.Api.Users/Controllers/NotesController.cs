using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NTA.Api.Contracts;
using NTA.Api.Users.Mappings;
using NTA.Core.Authentication;
using NTA.Core.Context;
using NTA.Core.ValueObjects;
using NTA.Domain.Exceptions;
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
            SetEtag(note.Revision);
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

            SetEtag(note.Revision);
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
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
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

            if (!TryGetExpectedRevision(out var expectedRevision))
            {
                return PreconditionError();
            }

            note.Update(
                request.Title,
                request.Content,
                timeProvider.GetUtcNow());
            var nextRevision = await repository.UpdateAsync(note, expectedRevision, cancellationToken);

            SetEtag(nextRevision);
            return Ok(NoteContractMapper.ToContract(note));
        }
        catch (NoteConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new { error = "The note has been modified." });
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
    [ProducesResponseType(StatusCodes.Status412PreconditionFailed)]
    [ProducesResponseType(StatusCodes.Status428PreconditionRequired)]
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

            if (!TryGetExpectedRevision(out var expectedRevision))
            {
                return PreconditionError();
            }

            await repository.DeleteAsync(note.Id, expectedRevision, cancellationToken);
            await policyRepository.DeleteAsync(note.Id, cancellationToken);
            return NoContent();
        }
        catch (NoteConcurrencyException)
        {
            return StatusCode(StatusCodes.Status412PreconditionFailed, new { error = "The note has been modified." });
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

    private bool TryGetExpectedRevision(out long expectedRevision) =>
        NoteEtag.TryParseExpectedRevision(Request.Headers.IfMatch, out expectedRevision);

    private ObjectResult PreconditionError() =>
        Request.Headers.ContainsKey("If-Match")
            ? BadRequest(new { error = "The If-Match header is malformed." })
            : StatusCode(StatusCodes.Status428PreconditionRequired, new { error = "An If-Match header is required." });
    private void SetEtag(long revision) => Response.Headers.ETag = NoteEtag.Format(revision);
}
