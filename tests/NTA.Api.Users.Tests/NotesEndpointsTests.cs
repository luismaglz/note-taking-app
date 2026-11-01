using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Mvc.Testing;
using NTA.Api.Contracts;

namespace NTA.Api.Users.Tests;

/// <summary>
/// Verifies the note CRUD HTTP contract end to end.
/// </summary>
public sealed class NotesEndpointsTests(NotesApiFactory factory) : IClassFixture<NotesApiFactory>
{
    private static readonly JsonSerializerOptions JsonOptions = CreateJsonOptions();

    [Fact]
    public async Task NoteCrud_CreatesReadsUpdatesAndDeletesAnOwnedNote()
    {
        using var client = factory.CreateClient();
        var unauthorizedResponse = await client.GetAsync("/notes");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);

        var authenticationResponse = await client.PostAsync("/auth/7", null);
        var authentication = await authenticationResponse.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.Equal(HttpStatusCode.OK, authenticationResponse.StatusCode);
        Assert.NotNull(authentication);

        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);

        var createResponse = await client.PostAsJsonAsync(
            "/notes",
            new CreateNoteRequest("Draft", "Initial content"));

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);

        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(created);

        var listResponse = await client.GetAsync("/notes");
        var notes = await listResponse.Content.ReadFromJsonAsync<NoteResponse[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, listResponse.StatusCode);
        Assert.Contains(notes!, note => note.Id == created.Id);

        var readResponse = await client.GetAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.OK, readResponse.StatusCode);

        var updateResponse = await client.PutAsJsonAsync(
            $"/notes/{created.Id}",
            new UpdateNoteRequest("Published", "Updated content"));

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);

        var updated = await updateResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);
        Assert.NotNull(updated);
        Assert.Equal("Published", updated.Title);
        var deleteResponse = await client.DeleteAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        var afterDeleteResponse = await client.GetAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.NotFound, afterDeleteResponse.StatusCode);
    }

    [Fact]
    public async Task List_ReturnsOwnedAndReadSharedNotesWithoutDuplicates()
    {
        using var owner = factory.CreateClient();
        using var member = factory.CreateClient();
        using var nonMember = factory.CreateClient();
        await AuthenticateAsync(owner, 1);
        await AuthenticateAsync(member, 2);
        await AuthenticateAsync(nonMember, 4);

        var ownedNote = await CreateNoteAsync(member, "Owned");
        var directlySharedNote = await CreateNoteAsync(owner, "Direct");
        var teamSharedNote = await CreateNoteAsync(owner, "Team");
        var multiplySharedNote = await CreateNoteAsync(owner, "Multiple");
        var inaccessibleNote = await CreateNoteAsync(owner, "Private");
        await GrantAsync(owner, directlySharedNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Read);
        await GrantAsync(owner, teamSharedNote.Id, AccessPrincipalKindContract.Team, 1, NotePermissionContract.Read);
        await GrantAsync(owner, multiplySharedNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Read);
        await GrantAsync(owner, multiplySharedNote.Id, AccessPrincipalKindContract.Team, 1, NotePermissionContract.Read);

        var memberNotes = await ListNotesAsync(member);

        Assert.Contains(memberNotes, note => note.Id == ownedNote.Id);
        Assert.Contains(memberNotes, note => note.Id == directlySharedNote.Id);
        Assert.Contains(memberNotes, note => note.Id == teamSharedNote.Id);
        Assert.Equal(1, memberNotes.Count(note => note.Id == multiplySharedNote.Id));
        Assert.DoesNotContain(memberNotes, note => note.Id == inaccessibleNote.Id);

        var ownerNotes = await ListNotesAsync(owner);

        Assert.Contains(ownerNotes, note => note.Id == directlySharedNote.Id);
        Assert.Contains(ownerNotes, note => note.Id == teamSharedNote.Id);
        Assert.Contains(ownerNotes, note => note.Id == multiplySharedNote.Id);
        Assert.Contains(ownerNotes, note => note.Id == inaccessibleNote.Id);

        var nonMemberNotes = await ListNotesAsync(nonMember);

        Assert.DoesNotContain(nonMemberNotes, note => note.Id == directlySharedNote.Id);
        Assert.DoesNotContain(nonMemberNotes, note => note.Id == teamSharedNote.Id);
        Assert.DoesNotContain(nonMemberNotes, note => note.Id == multiplySharedNote.Id);
    }

    [Fact]
    public async Task AccessGrants_AuthorizeDirectUsersAndTeamMembersByPermission()
    {
        using var owner = factory.CreateClient();
        using var directUser = factory.CreateClient();
        using var nonMember = factory.CreateClient();
        await AuthenticateAsync(owner, 1);
        await AuthenticateAsync(directUser, 2);
        await AuthenticateAsync(nonMember, 4);

        var directNote = await CreateNoteAsync(owner, "Direct");

        Assert.Equal(HttpStatusCode.NotFound, (await directUser.GetAsync($"/notes/{directNote.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await directUser.PutAsJsonAsync($"/notes/{directNote.Id}", new UpdateNoteRequest("Denied", "Denied"))).StatusCode);

        await GrantAsync(owner, directNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Read);
        Assert.Equal(HttpStatusCode.OK, (await directUser.GetAsync($"/notes/{directNote.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Forbidden,
            (await directUser.PutAsJsonAsync($"/notes/{directNote.Id}", new UpdateNoteRequest("Denied", "Denied"))).StatusCode);

        await GrantAsync(owner, directNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Write);
        Assert.Equal(
            HttpStatusCode.OK,
            (await directUser.PutAsJsonAsync($"/notes/{directNote.Id}", new UpdateNoteRequest("Permitted", "Updated"))).StatusCode);

        await GrantAsync(owner, directNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Delete);
        Assert.Equal(HttpStatusCode.NoContent, (await directUser.DeleteAsync($"/notes/{directNote.Id}")).StatusCode);

        var teamNote = await CreateNoteAsync(owner, "Team");
        await GrantAsync(owner, teamNote.Id, AccessPrincipalKindContract.Team, 1, NotePermissionContract.Read);

        Assert.Equal(HttpStatusCode.OK, (await directUser.GetAsync($"/notes/{teamNote.Id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await nonMember.GetAsync($"/notes/{teamNote.Id}")).StatusCode);
        Assert.Equal(
            HttpStatusCode.Conflict,
            (await owner.PostAsJsonAsync(
                $"/notes/{teamNote.Id}/access-grants",
                new CreateNoteAccessGrantRequest(
                    AccessPrincipalKindContract.Team,
                    1,
                    NotePermissionContract.Read))).StatusCode);
        Assert.Equal(
            HttpStatusCode.BadRequest,
            (await owner.PostAsJsonAsync(
                $"/notes/{teamNote.Id}/access-grants",
                new CreateNoteAccessGrantRequest(
                    AccessPrincipalKindContract.User,
                    0,
                    NotePermissionContract.Read))).StatusCode);
    }

    private static async Task AuthenticateAsync(HttpClient client, int userId)
    {
        var response = await client.PostAsync($"/auth/{userId}", null);
        var authentication = await response.Content.ReadFromJsonAsync<AuthenticationResponse>();

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.NotNull(authentication);
        client.DefaultRequestHeaders.Authorization =
            new AuthenticationHeaderValue("Bearer", authentication.AccessToken);
    }

    private static async Task<NoteResponse> CreateNoteAsync(HttpClient client, string title)
    {
        var response = await client.PostAsJsonAsync("/notes", new CreateNoteRequest(title, "Content"));
        var note = await response.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return Assert.IsType<NoteResponse>(note);
    }

    private static async Task<NoteResponse[]> ListNotesAsync(HttpClient client)
    {
        var response = await client.GetAsync("/notes");
        var notes = await response.Content.ReadFromJsonAsync<NoteResponse[]>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return Assert.IsType<NoteResponse[]>(notes);
    }

    private static async Task GrantAsync(
        HttpClient client,
        Guid noteId,
        AccessPrincipalKindContract principalKind,
        int principalId,
        NotePermissionContract permission)
    {
        var response = await client.PostAsJsonAsync(
            $"/notes/{noteId}/access-grants",
            new CreateNoteAccessGrantRequest(principalKind, principalId, permission));

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    private static JsonSerializerOptions CreateJsonOptions()
    {
        var options = new JsonSerializerOptions(JsonSerializerDefaults.Web);
        options.Converters.Add(new JsonStringEnumConverter());
        return options;
    }
}
