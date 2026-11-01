using Microsoft.AspNetCore.Mvc;
using NTA.Api.Contracts;
using NTA.Api.Users.Mappings;
using NTA.Core.ValueObjects;
using NTA.Domain.Interfaces;
using NTA.Domain.Models;

namespace NTA.Api.Users.Controllers;

/// <summary>
/// Provides CRUD operations for user notes.
/// </summary>
[ApiController]
[Route("notes")]
public sealed class NotesController(INoteRepository repository, TimeProvider timeProvider) : ControllerBase
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
                new UserId(request.OwnerId),
                request.Title,
                request.Content,
                NoteContractMapper.ToDomain(request.Visibility),
                request.TeamId is null ? null : new TeamId(request.TeamId.Value),
                timeProvider.GetUtcNow());

            await repository.AddAsync(note, cancellationToken);
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
    public async Task<ActionResult<IReadOnlyList<NoteResponse>>> List(
        [FromQuery] int userId,
        [FromQuery] int[]? teamIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var accessibleNotes = (await repository.ListAsync(cancellationToken))
                .Where(note => note.CanBeViewedBy(new UserId(userId), CreateTeamIds(teamIds)))
                .Select(NoteContractMapper.ToContract)
                .ToArray();

            return Ok(accessibleNotes);
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
    public async Task<ActionResult<NoteResponse>> Get(
        Guid noteId,
        [FromQuery] int userId,
        [FromQuery] int[]? teamIds,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null || !note.CanBeViewedBy(new UserId(userId), CreateTeamIds(teamIds)))
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
        [FromQuery] int userId,
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

            if (note.OwnerId != new UserId(userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            note.Update(
                request.Title,
                request.Content,
                NoteContractMapper.ToDomain(request.Visibility),
                request.TeamId is null ? null : new TeamId(request.TeamId.Value),
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
    public async Task<IActionResult> Delete(
        Guid noteId,
        [FromQuery] int userId,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await repository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null)
            {
                return NotFound();
            }

            if (note.OwnerId != new UserId(userId))
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            await repository.DeleteAsync(note.Id, cancellationToken);
            return NoContent();
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }

    private static HashSet<TeamId> CreateTeamIds(IEnumerable<int>? values) =>
        values?.Select(value => new TeamId(value)).ToHashSet() ?? [];
}
