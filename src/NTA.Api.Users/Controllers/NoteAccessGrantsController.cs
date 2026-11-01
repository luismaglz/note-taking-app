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
/// Provides the minimal owner-managed API for granting note access.
/// </summary>
[ApiController]
[Route("notes/{noteId:guid}/access-grants")]
[Authorize(Roles = UserRoles.User)]
public sealed class NoteAccessGrantsController(
    INoteRepository noteRepository,
    INoteAccessPolicyRepository policyRepository,
    UserContext userContext) : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<NoteAccessGrantResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    public async Task<ActionResult<NoteAccessGrantResponse>> Create(
        Guid noteId,
        CreateNoteAccessGrantRequest request,
        CancellationToken cancellationToken)
    {
        try
        {
            var note = await noteRepository.GetByIdAsync(new NoteId(noteId), cancellationToken);
            if (note is null)
            {
                return NotFound();
            }

            if (note.OwnerId != userContext.GetRequiredUserId())
            {
                return StatusCode(StatusCodes.Status403Forbidden);
            }

            var grant = NoteAccessGrantContractMapper.ToDomain(request);
            var policy = await policyRepository.GetByNoteIdAsync(note.Id, cancellationToken);
            if (policy is null)
            {
                policy = NoteAccessPolicy.Create(note.Id);
                policy.GrantAccess(grant);
                await policyRepository.AddAsync(policy, cancellationToken);
            }
            else
            {
                policy.GrantAccess(grant);
                await policyRepository.UpdateAsync(policy, cancellationToken);
            }

            return Created(
                $"/notes/{note.Id.Value}/access-grants",
                NoteAccessGrantContractMapper.ToContract(grant));
        }
        catch (InvalidOperationException exception)
        {
            return Conflict(new { error = exception.Message });
        }
        catch (ArgumentException exception)
        {
            return BadRequest(new { error = exception.Message });
        }
    }
}
