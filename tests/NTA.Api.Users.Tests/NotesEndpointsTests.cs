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
    public async Task CreateAndGet_IssueTheInitialStrongEtag()
    {
        using var unauthorizedClient = factory.CreateClient();
        var unauthorizedResponse = await unauthorizedClient.GetAsync("/notes");
        using var client = await CreateAuthenticatedClientAsync();

        var (created, etag) = await CreateNoteAsync(client);
        var getResponse = await client.GetAsync($"/notes/{created.Id}");

        Assert.Equal(HttpStatusCode.Unauthorized, unauthorizedResponse.StatusCode);
        Assert.Equal("\"1\"", etag);
        Assert.Equal(HttpStatusCode.OK, getResponse.StatusCode);
        Assert.Equal(etag, getResponse.Headers.ETag?.Tag);
        Assert.False(getResponse.Headers.ETag?.IsWeak ?? true);
    }

    [Fact]
    public async Task Update_WithCurrentEtag_ChangesTheNoteAndAdvancesItsEtag()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (created, etag) = await CreateNoteAsync(client);

        var updateResponse = await client.SendAsync(CreateUpdateRequest(
            created.Id,
            new UpdateNoteRequest("Published", "Updated content"),
            etag));
        var updated = await updateResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.NotNull(updated);
        Assert.Equal("Published", updated.Title);
        Assert.Equal("\"2\"", updateResponse.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task Update_WithStaleEtag_IsRejectedAndCanBeRetriedWithTheCurrentEtag()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (created, initialEtag) = await CreateNoteAsync(client);

        var firstUpdate = await client.SendAsync(CreateUpdateRequest(
            created.Id,
            new UpdateNoteRequest("First update", "Updated content"),
            initialEtag));
        var currentEtag = firstUpdate.Headers.ETag?.Tag;

        var staleUpdate = await client.SendAsync(CreateUpdateRequest(
            created.Id,
            new UpdateNoteRequest("Stale update", "Updated content"),
            initialEtag));

        var retryUpdate = await client.SendAsync(CreateUpdateRequest(
            created.Id,
            new UpdateNoteRequest("Retried update", "Updated content"),
            currentEtag));

        Assert.Equal(HttpStatusCode.OK, firstUpdate.StatusCode);
        Assert.Equal("\"2\"", currentEtag);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleUpdate.StatusCode);
        Assert.Equal(HttpStatusCode.OK, retryUpdate.StatusCode);
        Assert.Equal("\"3\"", retryUpdate.Headers.ETag?.Tag);
    }

    [Fact]
    public async Task Delete_RequiresAValidCurrentEtag()
    {
        using var client = await CreateAuthenticatedClientAsync();
        var (created, initialEtag) = await CreateNoteAsync(client);

        var absentPrecondition = await client.DeleteAsync($"/notes/{created.Id}");

        using var malformedRequest = new HttpRequestMessage(HttpMethod.Delete, $"/notes/{created.Id}");
        malformedRequest.Headers.TryAddWithoutValidation("If-Match", "W/\"1\"");
        var malformedPrecondition = await client.SendAsync(malformedRequest);

        var updateResponse = await client.SendAsync(CreateUpdateRequest(
            created.Id,
            new UpdateNoteRequest("Changed", "Updated content"),
            initialEtag));

        using var staleDeleteRequest = CreateDeleteRequest(created.Id, initialEtag);
        var staleDelete = await client.SendAsync(staleDeleteRequest);

        using var deleteRequest = CreateDeleteRequest(created.Id, updateResponse.Headers.ETag?.Tag);
        var deleteResponse = await client.SendAsync(deleteRequest);

        Assert.Equal(HttpStatusCode.PreconditionRequired, absentPrecondition.StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, malformedPrecondition.StatusCode);
        Assert.Equal(HttpStatusCode.OK, updateResponse.StatusCode);
        Assert.Equal(HttpStatusCode.PreconditionFailed, staleDelete.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
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
        await GrantAsync(owner, directlySharedNote.Note.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Read);
        await GrantAsync(owner, teamSharedNote.Note.Id, AccessPrincipalKindContract.Team, 1, NotePermissionContract.Read);
        await GrantAsync(owner, multiplySharedNote.Note.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Read);
        await GrantAsync(owner, multiplySharedNote.Note.Id, AccessPrincipalKindContract.Team, 1, NotePermissionContract.Read);

        var memberNotes = await ListNotesAsync(member);

        Assert.Contains(memberNotes, note => note.Id == ownedNote.Note.Id);
        Assert.Contains(memberNotes, note => note.Id == directlySharedNote.Note.Id);
        Assert.Contains(memberNotes, note => note.Id == teamSharedNote.Note.Id);
        Assert.Equal(1, memberNotes.Count(note => note.Id == multiplySharedNote.Note.Id));
        Assert.DoesNotContain(memberNotes, note => note.Id == inaccessibleNote.Note.Id);

        var ownerNotes = await ListNotesAsync(owner);

        Assert.Contains(ownerNotes, note => note.Id == directlySharedNote.Note.Id);
        Assert.Contains(ownerNotes, note => note.Id == teamSharedNote.Note.Id);
        Assert.Contains(ownerNotes, note => note.Id == multiplySharedNote.Note.Id);
        Assert.Contains(ownerNotes, note => note.Id == inaccessibleNote.Note.Id);

        var nonMemberNotes = await ListNotesAsync(nonMember);

        Assert.DoesNotContain(nonMemberNotes, note => note.Id == directlySharedNote.Note.Id);
        Assert.DoesNotContain(nonMemberNotes, note => note.Id == teamSharedNote.Note.Id);
        Assert.DoesNotContain(nonMemberNotes, note => note.Id == multiplySharedNote.Note.Id);
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

        var (directNote, directNoteEtag) = await CreateNoteAsync(owner, "Direct");

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
        var directUpdate = await directUser.SendAsync(CreateUpdateRequest(
            directNote.Id,
            new UpdateNoteRequest("Permitted", "Updated"),
            directNoteEtag));
        Assert.Equal(HttpStatusCode.OK, directUpdate.StatusCode);

        await GrantAsync(owner, directNote.Id, AccessPrincipalKindContract.User, 2, NotePermissionContract.Delete);
        using var directDeleteRequest = CreateDeleteRequest(directNote.Id, directUpdate.Headers.ETag?.Tag);
        Assert.Equal(HttpStatusCode.NoContent, (await directUser.SendAsync(directDeleteRequest)).StatusCode);

        var (teamNote, _) = await CreateNoteAsync(owner, "Team");
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

    private static HttpRequestMessage CreateDeleteRequest(Guid noteId, string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Delete, $"/notes/{noteId}");
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static HttpRequestMessage CreateUpdateRequest(
        Guid noteId,
        UpdateNoteRequest update,
        string? etag)
    {
        var request = new HttpRequestMessage(HttpMethod.Put, $"/notes/{noteId}")
        {
            Content = JsonContent.Create(update)
        };
        request.Headers.TryAddWithoutValidation("If-Match", etag);
        return request;
    }

    private static async Task<(NoteResponse Note, string Etag)> CreateNoteAsync(
        HttpClient client,
        string title = "Draft")
    {
        var createResponse = await client.PostAsJsonAsync("/notes", new CreateNoteRequest(title, "Initial content"));
        var created = await createResponse.Content.ReadFromJsonAsync<NoteResponse>(JsonOptions);

        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        Assert.NotNull(created);
        Assert.NotNull(createResponse.Headers.ETag);

        return (created, createResponse.Headers.ETag.Tag);
    }

    private async Task<HttpClient> CreateAuthenticatedClientAsync()
    {
        var client = factory.CreateClient();
        await AuthenticateAsync(client, 7);
        return client;
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
